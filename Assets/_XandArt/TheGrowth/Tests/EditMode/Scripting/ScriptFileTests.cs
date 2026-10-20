using System;
using System.Reflection;
using NUnit.Framework;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Scripting
{
    public sealed class ScriptFileTests
    {
        [TestCase("Start", null, "Start")]
        [TestCase("dialog.Start", "dialog", "Start")]
        [TestCase("Cards/Lucia.v2.OnPlaced", "Cards/Lucia.v2", "OnPlaced")]
        [TestCase("  file.Section ", "file", "Section")]
        public void SectionRef_Parse(string text, string file, string section)
        {
            var reference = SectionRef.Parse(text);

            Assert.AreEqual(file, reference.File);
            Assert.AreEqual(section, reference.Section);
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(".Start")]
        [TestCase("file.")]
        public void SectionRef_ParseInvalid_Throws(string text)
        {
            Assert.Throws<FormatException>(() => SectionRef.Parse(text));
        }

        [Test]
        public void SectionRef_ToString_RoundTrips()
        {
            Assert.AreEqual("Start", new SectionRef("Start").ToString());
            Assert.AreEqual("file.Start", new SectionRef("file", "Start").ToString());
        }

        [Test]
        public void ScriptFile_FindsSectionsByName()
        {
            var start = new ScriptSection("Start", null);
            var file = new ScriptFile("test", new[] { start, new ScriptSection("End", null) });

            Assert.IsTrue(file.TryGetSection("Start", out var found));
            Assert.AreSame(start, found);
            Assert.IsFalse(file.TryGetSection("Missing", out _));
            Assert.IsFalse(file.TryGetSection(null, out _));
            Assert.AreEqual(2, file.Sections.Count);
            Assert.AreEqual(0, start.Commands.Count);
        }

        [Test]
        public void ScriptFile_DuplicateSection_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ScriptFile("test", new[] { new ScriptSection("A", null), new ScriptSection("A", null) }));
        }

        [Test]
        public void ScriptSection_NameWithDot_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ScriptSection("file.Start", null));
        }

        [Test]
        public void ScriptLocation_ToString()
        {
            Assert.AreEqual("cards/lucia:12", new ScriptLocation("cards/lucia", 12).ToString());
            Assert.IsFalse(default(ScriptLocation).IsValid);
        }

        [TestCase(typeof(Branch), "if", "if [Condition] {OnTrue} else {OnFalse}")]
        [TestCase(typeof(Goto), "goto", "goto [Target]")]
        [TestCase(typeof(Call), "call", "call [Target]")]
        [TestCase(typeof(Return), "return", "return")]
        [TestCase(typeof(Parallel), "parallel", "parallel {Commands}")]
        [TestCase(typeof(Background), "background", "background {Commands}")]
        [TestCase(typeof(DebugLog), "log", "log [Message]")]
        public void BuiltInCommands_HaveScriptAttribute(Type type, string name, string template)
        {
            var attribute = type.GetCustomAttribute<ScriptAttribute>();

            Assert.IsNotNull(attribute);
            Assert.AreEqual(name, attribute.Name);
            Assert.AreEqual(template, attribute.Template);
        }
    }
}

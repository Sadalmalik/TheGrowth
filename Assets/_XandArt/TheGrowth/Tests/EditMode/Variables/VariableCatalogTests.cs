using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Variables;
using TheGrowth.EditorTools.Content;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Locations;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Variables
{
    /// <summary>Объявления переменных: сбор из конфигов, поиск по имени и прежнему имени, проверки, валидатор контента.</summary>
    public sealed class VariableCatalogTests
    {
        private ContentTestKit m_Kit;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        private static string Errors(VariableCatalog catalog)
        {
            return string.Join("\n", catalog.Errors);
        }

        private static void AssertError(VariableCatalog catalog, string variable, string fragment)
        {
            Assert.IsTrue(catalog.Errors.Any(error => error.Variable == variable && error.Message.Contains(fragment)),
                $"No error of '{variable}' containing '{fragment}':\n{Errors(catalog)}");
        }

        [Test]
        public void Catalog_CollectsConfigs_FindsByNameAndPreviousName()
        {
            var story = VariableTestKit.Declarations(m_Kit, "Story",
                new VariableDeclaration("trust_lucia", VariableScope.Game, VariableType.Int, "3", "Lucia's trust"),
                new VariableDeclaration("gold", VariableScope.Game, VariableType.Int, null, null, "money", "cash"));
            var endings = VariableTestKit.Declarations(m_Kit, "Endings",
                new VariableDeclaration("ending_seen", VariableScope.Profile, VariableType.Bool, "true"));

            var catalog = new VariableCatalog(new[] { story, endings });

            Assert.IsEmpty(catalog.Errors, Errors(catalog));
            Assert.That(catalog.Declarations.Select(declaration => declaration.Name), Is.EqualTo(new[] { "trust_lucia", "gold", "ending_seen" }));

            var trust = catalog.Get("trust_lucia");
            Assert.AreEqual(VariableScope.Game, trust.Scope);
            Assert.AreEqual(typeof(int), trust.ValueType);
            Assert.AreEqual(3, trust.DefaultValue);
            Assert.AreEqual("Lucia's trust", trust.Description);
            Assert.AreEqual(true, catalog.Get("ending_seen").DefaultValue);

            Assert.IsTrue(catalog.TryGetRenamed("cash", out var renamed));
            Assert.AreSame(catalog.Get("gold"), renamed);
            Assert.IsFalse(catalog.TryGet("money", out _), "a previous name is not a current name");
            Assert.IsFalse(catalog.TryGetRenamed("gold", out _));
            Assert.IsFalse(catalog.TryGet("nope", out _));
        }

        [Test]
        public void FromContent_UsesDeclarationsOfRegistry()
        {
            var declarations = VariableTestKit.Declarations(m_Kit, "Variables", VariableTestKit.Standard());
            var registry = m_Kit.Registry(new ContentAsset[] { LocationTestKit.Location(m_Kit, "hub", "HubScene"), declarations });

            var catalog = VariableCatalog.FromContent(registry);

            Assert.IsEmpty(catalog.Errors, Errors(catalog));
            Assert.AreEqual(VariableTestKit.Standard().Length, catalog.Declarations.Count);
            Assert.AreEqual(VariableScope.Card, catalog.Get("charges").Scope);
        }

        [Test]
        public void DuplicateName_InAnotherConfigOrScope_Error_FirstWins()
        {
            var first = VariableTestKit.Declarations(m_Kit, "First", new VariableDeclaration("coins", VariableScope.Game, VariableType.Int, "1"));
            var second = VariableTestKit.Declarations(m_Kit, "Second", new VariableDeclaration("coins", VariableScope.Card, VariableType.Bool));

            var catalog = new VariableCatalog(new[] { first, second });

            Assert.AreEqual(1, catalog.Errors.Count, Errors(catalog));
            AssertError(catalog, "coins", "declared twice (also in 'First')");
            Assert.AreSame(second, catalog.Errors[0].Source);
            Assert.AreEqual(VariableScope.Game, catalog.Get("coins").Scope);
        }

        [Test]
        public void PreviousNames_ClashWithNameOrAnotherPreviousName_Error()
        {
            var catalog = VariableTestKit.Catalog(m_Kit,
                new VariableDeclaration("gold", VariableScope.Game, VariableType.Int, null, null, "money", "coins", "gold"),
                new VariableDeclaration("coins", VariableScope.Game, VariableType.Int, null, null, "money", "1bad"));

            AssertError(catalog, "gold", "previous name 'coins' is the name of a declared variable");
            AssertError(catalog, "gold", "previous name 'gold' is the name of a declared variable");
            AssertError(catalog, "coins", "previous name 'money' is also a previous name of 'gold'");
            AssertError(catalog, "coins", "previous name '1bad' is not an identifier");
            Assert.AreEqual(4, catalog.Errors.Count, Errors(catalog));
            Assert.AreSame(catalog.Get("gold"), catalog.TryGetRenamed("money", out var renamed) ? renamed : null, "the first previous name wins");
        }

        [TestCase("")]
        [TestCase("1coins")]
        [TestCase("my coins")]
        [TestCase("my-coins")]
        [TestCase("$coins")]
        public void InvalidName_Error_NotInCatalog(string name)
        {
            var catalog = VariableTestKit.Catalog(m_Kit, new VariableDeclaration(name, VariableScope.Game, VariableType.Int));

            Assert.AreEqual(1, catalog.Errors.Count, Errors(catalog));
            StringAssert.Contains(name == "" ? "name is empty" : "is not an identifier", catalog.Errors[0].Message);
            Assert.IsEmpty(catalog.Declarations);
        }

        [TestCase(VariableType.Bool, "yes")]
        [TestCase(VariableType.Bool, "True")]
        [TestCase(VariableType.Int, "1.5")]
        [TestCase(VariableType.Int, "ten")]
        [TestCase(VariableType.Int, "99999999999")]
        public void InvalidDefault_Error_VariableKeptWithZeroValue(VariableType type, string text)
        {
            var catalog = VariableTestKit.Catalog(m_Kit, new VariableDeclaration("x", VariableScope.Game, type, text));

            Assert.AreEqual(1, catalog.Errors.Count, Errors(catalog));
            AssertError(catalog, "x", $"default value '{text}' is not a valid {type.ToText()}");
            Assert.AreEqual(type.GetZeroValue(), catalog.Get("x").DefaultValue);
        }

        [TestCase(VariableType.Bool, "true", true)]
        [TestCase(VariableType.Bool, " false ", false)]
        [TestCase(VariableType.Bool, "", false)]
        [TestCase(VariableType.Int, "-15", -15)]
        [TestCase(VariableType.Int, null, 0)]
        [TestCase(VariableType.String, " Ann ", " Ann ")]
        [TestCase(VariableType.String, null, "")]
        public void Default_ParsedByType(VariableType type, string text, object expected)
        {
            var declaration = new VariableDeclaration("x", VariableScope.Game, type, text);

            Assert.IsTrue(declaration.TryParseDefault(out var value, out var error), error);
            Assert.AreEqual(expected, value);
            Assert.AreEqual(expected, declaration.DefaultValue);
        }

        [Test]
        public void Init_InContainer_LogsErrors()
        {
            var catalog = VariableTestKit.Catalog(m_Kit, new VariableDeclaration("bad name", VariableScope.Game, VariableType.Int));
            var scope = new LifetimeScope("Global");
            scope.Container.Add(catalog);

            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("[Variables] Variables: variable 'bad name': name is not an identifier")));
            scope.Container.Init();
            scope.Dispose();
        }

        [Test]
        public void ContentValidator_ReportsDeclarationErrors()
        {
            var declarations = VariableTestKit.Declarations(m_Kit, "Variables",
                new VariableDeclaration("coins", VariableScope.Game, VariableType.Int, "many"),
                new VariableDeclaration("coins", VariableScope.Game, VariableType.Int));

            var errors = ContentValidator.Validate(new ContentAsset[] { declarations }, null);

            Assert.AreEqual(2, errors.Count, string.Join("\n", errors));
            Assert.That(errors.Select(error => error.Kind), Has.All.EqualTo(ContentErrorKind.InvalidVariable));
            Assert.AreEqual("<memory:Variables>", errors[0].Path);
            StringAssert.Contains("variable 'coins': default value 'many'", errors[0].Message);
            StringAssert.Contains("declared twice", errors[1].Message);
            Assert.AreSame(declarations, errors[0].Context);
        }

        [Test]
        public void VariablesHash_ChangesWithNamesScopesTypesPreviousNames_NotWithDefaults()
        {
            string Hash(params VariableDeclaration[] variables)
            {
                return ContentRegistryBuilder.ComputeVariablesHash(m_Kit.Registry(new ContentAsset[] { VariableTestKit.Declarations(m_Kit, "Variables", variables) })).ToString();
            }

            var baseline = Hash(new VariableDeclaration("coins", VariableScope.Game, VariableType.Int, "1", "a"));

            Assert.AreEqual(baseline, Hash(new VariableDeclaration("coins", VariableScope.Game, VariableType.Int, "5", "b")));
            Assert.AreNotEqual(baseline, Hash(new VariableDeclaration("gold", VariableScope.Game, VariableType.Int, "1", "a")));
            Assert.AreNotEqual(baseline, Hash(new VariableDeclaration("coins", VariableScope.Profile, VariableType.Int, "1", "a")));
            Assert.AreNotEqual(baseline, Hash(new VariableDeclaration("coins", VariableScope.Game, VariableType.String, "1", "a")));
            Assert.AreNotEqual(baseline, Hash(new VariableDeclaration("coins", VariableScope.Game, VariableType.Int, "1", "a", "money")));
        }
    }
}

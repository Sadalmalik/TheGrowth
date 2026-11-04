using System;
using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Scripting
{
#region Test extension

    public sealed class TestBag
    {
        public int Value;
    }

    /// <summary>int в мешок, тип значения мешка, свойство <c>Bag</c>.</summary>
    public sealed class TestBagLanguage : ScriptExtension
    {
        public override void Declare(ScriptDeclarations declarations)
        {
            declarations.AddConversion<int, TestBag>(value => new TestBag { Value = value });
            declarations.AddValueType<TestBag>();
            declarations.AddProperty<TestBag>("Bag");
        }
    }

    public sealed class BrokenPropertiesLanguage : ScriptExtension
    {
        public override void Declare(ScriptDeclarations declarations)
        {
            declarations.AddProperty<int>("not valid");
            declarations.AddProperty<int>("Twice");
            declarations.AddProperty<int>("Twice");
        }
    }

    [Script("test_bag", "bag of [Value]")]
    public sealed class BagEvaluator : Evaluator<TestBag>
    {
        public int Value;

        public override TestBag Evaluate(ScriptContext context)
        {
            return new TestBag { Value = Value };
        }
    }

    [Script("test_bag_size", "size of [Bag]")]
    public sealed class BagSizeEvaluator : Evaluator<int>
    {
        public Evaluator<TestBag> Bag;

        public override int Evaluate(ScriptContext context)
        {
            return Bag.Evaluate(context).Value;
        }
    }

    /// <summary>Скелет как у <see cref="BagEvaluator"/>, результат int: при преобразовании int → мешок — конфликт.</summary>
    [Script("test_int_bag", "bag of [Value]")]
    public sealed class IntBagEvaluator : Evaluator<int>
    {
        public int Value;

        public override int Evaluate(ScriptContext context)
        {
            return Value;
        }
    }

#endregion

    /// <summary>Расширения языка (<see cref="ScriptExtension"/>): свойства, преобразования, проверки реестра.</summary>
    public sealed class ScriptExtensionTests
    {
        private static ScriptRegistry Build(params Type[] types)
        {
            return ScriptRegistry.Build(ScriptLanguageTypes.All().Concat(types));
        }

        [Test]
        public void Extension_DeclaresPropertyAndConversion()
        {
            var registry = Build(typeof(TestBagLanguage), typeof(BagEvaluator), typeof(BagSizeEvaluator));

            Assert.IsTrue(registry.TryGetProperty("Bag", out var bag));
            Assert.AreEqual(typeof(TestBag), bag.Type);
            Assert.IsTrue(registry.TryGetConversion(typeof(int), typeof(TestBag), out var conversion));
            Assert.AreEqual(typeof(TestBag), conversion.To);
        }

        [Test]
        public void Conversion_ParsedAsWrappedNode_PrintedAsSource()
        {
            var registry = Build(typeof(TestBagLanguage), typeof(BagEvaluator), typeof(BagSizeEvaluator));
            var parser = new ScriptParser(registry);
            var text = "Bag -> [sum of 1, 2]\n\nS:\n    check [size of [bag of 3]]\n    check [size of [sum of 4, 5]]\n";

            var result = parser.Parse("test", ScriptFileKind.Script, text);

            Assert.IsFalse(result.HasErrors, result.ToString());
            Assert.IsTrue(result.File.TryGetProperty("Bag", out var property));
            Assert.IsInstanceOf<ConversionValue<int, TestBag>>(property.Value);
            var size = (BagSizeEvaluator)((CheckCommand)result.File.Sections[0].Commands[1]).Value;
            Assert.IsInstanceOf<ConversionValue<int, TestBag>>(size.Bag);
            Assert.AreEqual(9, size.Evaluate(null));
            Assert.AreEqual(text, new ScriptPrinter(registry).Print(result.File));
        }

        [Test]
        public void Conversion_SameSkeletonAsTargetType_RegistryError()
        {
            var exception = Assert.Throws<ScriptRegistryException>(() => Build(typeof(TestBagLanguage), typeof(BagEvaluator), typeof(IntBagEvaluator)));
            StringAssert.Contains("same skeleton 'bag of []'", exception.Message);

            Assert.DoesNotThrow(() => Build(typeof(BagEvaluator), typeof(IntBagEvaluator)), "without the conversion the result types differ");
        }

        [Test]
        public void Properties_InvalidAndRepeatedNames_RegistryError()
        {
            var exception = Assert.Throws<ScriptRegistryException>(() => Build(typeof(BrokenPropertiesLanguage)));

            StringAssert.Contains("property 'not valid': name must be an identifier", exception.Message);
            StringAssert.Contains("property 'Twice' is declared twice", exception.Message);
        }

        [Test]
        public void CollectTypes_FindsExtensions()
        {
            var types = ScriptRegistry.CollectTypes(new[] { typeof(TestBagLanguage).Assembly }).ToList();

            Assert.Contains(typeof(TestBagLanguage), types);
            Assert.IsFalse(ScriptRegistry.IsExtension(typeof(ScriptExtension)), "abstract base is not an extension");
        }
    }
}

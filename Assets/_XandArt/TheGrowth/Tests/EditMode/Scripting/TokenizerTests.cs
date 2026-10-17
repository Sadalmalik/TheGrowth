using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Scripting.Text;

namespace TheGrowth.Tests.EditMode.Scripting
{
    public sealed class TokenizerTests
    {
        private static string Describe(string text)
        {
            return string.Join(" ", Tokenizer.Tokenize(text).Select(t => $"{t.Kind}:{t.Text}"));
        }

        [Test]
        public void Identifiers_NumbersAndPunctuation()
        {
            Assert.AreEqual("Identifier:move Identifier:card_1 Identifier:to Number:12 Number:0.5 Punctuation:, Punctuation:.",
                Describe("move card_1 to 12 0.5 , ."));
        }

        [Test]
        public void Identifiers_Unicode()
        {
            Assert.AreEqual("Identifier:Люсия Punctuation:: Identifier:привет", Describe("Люсия: привет"));
        }

        [Test]
        public void Minus_IsSeparateToken()
        {
            var tokens = Tokenizer.Tokenize("-5 - 3");

            Assert.AreEqual("Punctuation:- Number:5 Punctuation:- Number:3", Describe("-5 - 3"));
            Assert.AreEqual(tokens[0].End, tokens[1].Start, "'-5' is adjacent");
            Assert.AreNotEqual(tokens[2].End, tokens[3].Start, "'- 3' is not adjacent");
        }

        [Test]
        public void Number_DotWithoutDigits_IsPunctuation()
        {
            Assert.AreEqual("Number:1 Punctuation:. Identifier:x", Describe("1.x"));
        }

        [Test]
        public void Operators_MaximalMunch()
        {
            Assert.AreEqual("Punctuation:>= Punctuation:== Punctuation:!= Punctuation:<= Punctuation:> Punctuation:< Punctuation:+",
                Describe(">= == != <= > < +"));
        }

        [Test]
        public void Brackets_AreSeparateTokens()
        {
            Assert.AreEqual("Punctuation:[ Punctuation:[ Variable:a Punctuation:+ Number:1 Punctuation:] Punctuation:> Number:3 Punctuation:]",
                Describe("[[$a + 1] > 3]"));
        }

        [Test]
        public void Variable_NameWithoutDollar()
        {
            var tokens = Tokenizer.Tokenize("$trust_lucia");

            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenKind.Variable, tokens[0].Kind);
            Assert.AreEqual("trust_lucia", tokens[0].Text);
            Assert.AreEqual(12, tokens[0].Length);
        }

        [Test]
        public void Variable_WithoutName_IsError()
        {
            var tokens = Tokenizer.Tokenize("$ x");

            Assert.AreEqual(TokenKind.Error, tokens[0].Kind);
        }

        [Test]
        public void String_Escapes()
        {
            var tokens = Tokenizer.Tokenize("\"a \\\"b\\\" \\\\ c\\nd\" x");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual(TokenKind.String, tokens[0].Kind);
            Assert.AreEqual("a \"b\" \\ c\nd", tokens[0].Text);
            Assert.AreEqual(0, tokens[0].Start);
            Assert.AreEqual(17, tokens[0].Length);
        }

        [Test]
        public void String_UnknownEscape_IsError()
        {
            var tokens = Tokenizer.Tokenize("\"a\\tb\" x");

            Assert.AreEqual(TokenKind.Error, tokens[0].Kind);
            StringAssert.Contains("unknown escape", tokens[0].Text);
            Assert.AreEqual("x", tokens[1].Text, "tokenizing continues after the string");
        }

        [Test]
        public void String_Unterminated_IsError()
        {
            var tokens = Tokenizer.Tokenize("log \"abc");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual(TokenKind.Error, tokens[1].Kind);
            StringAssert.Contains("unterminated", tokens[1].Text);
        }

        [Test]
        public void QuoteString_RoundTrips()
        {
            const string value = "a \"b\" \\ c\nd";
            var tokens = Tokenizer.Tokenize(Tokenizer.QuoteString(value));

            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(value, tokens[0].Text);
        }

        [TestCase("Lucia: Wha... HOW? #k3f9a2", "k3f9a2", "Lucia: Wha... HOW?")]
        [TestCase("say hi   #ABC123xyz  ", "ABC123xyz", "say hi")]
        [TestCase("goto Start #abcdef", "abcdef", "goto Start")]
        public void LineId_AtEnd(string text, string id, string code)
        {
            Assert.IsTrue(ScriptSyntax.TryGetLineId(text, out var actual, out var length));
            Assert.AreEqual(id, actual);
            Assert.AreEqual(code, text.Substring(0, length));
        }

        [TestCase("say hi #abc12")]
        [TestCase("say hi#abcdef")]
        [TestCase("say \"#abcdef\"")]
        [TestCase("say #abc-def1")]
        [TestCase("#abcdef")]
        public void LineId_NotAnId(string text)
        {
            Assert.IsFalse(ScriptSyntax.TryGetLineId(text, out _, out var length));
            Assert.AreEqual(text.Length, length);
        }
    }
}

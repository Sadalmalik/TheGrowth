namespace TheGrowth.Core.Scripting.Text
{
    public enum TokenKind
    {
        /// <summary>Слово: ключевое слово шаблона, ID конфига, имя секции, значение enum, <c>true</c> / <c>false</c>.</summary>
        Identifier,

        /// <summary>Число без знака: <c>12</c>, <c>0.5</c>. Минус — отдельный токен <c>-</c>.</summary>
        Number,

        /// <summary>Строка в кавычках; <see cref="Token.Text"/> — значение после разбора экранирования.</summary>
        String,

        /// <summary><c>$имя</c>; <see cref="Token.Text"/> — имя без <c>$</c>.</summary>
        Variable,

        /// <summary>Знак или составной оператор (<c>&gt;=</c>, <c>==</c>, ...).</summary>
        Punctuation,

        /// <summary>Ошибка лексики; <see cref="Token.Text"/> — сообщение. Ошибкой разбора становится, только если токен не попал в текст до конца строки.</summary>
        Error,
    }

    /// <summary>
    /// Токен строки. Позиции — индексы в разбираемой строке.
    /// </summary>
    public readonly struct Token
    {
        public TokenKind Kind { get; }

        public string Text { get; }

        public int Start { get; }

        public int Length { get; }

        public int End => Start + Length;

        public Token(TokenKind kind, string text, int start, int length)
        {
            Kind = kind;
            Text = text;
            Start = start;
            Length = length;
        }

        public bool Is(TokenKind kind, string text)
        {
            return Kind == kind && Text == text;
        }

        public bool IsPunctuation(string text)
        {
            return Kind == TokenKind.Punctuation && Text == text;
        }

        public bool SameAs(Token other)
        {
            return Kind == other.Kind && Text == other.Text;
        }

        public override string ToString()
        {
            return $"{Kind} '{Text}' at {Start}";
        }
    }
}

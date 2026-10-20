using FluentValidation.Results;

namespace ClefCraft.Application.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {

        }

        public BadRequestException(string message, ValidationResult validationResult) : base(message)
        {
            ValidationErrors = validationResult.ToDictionary();
        }

        /// <summary>A rejection the client reacts to specifically, identified by <paramref name="code"/>.</summary>
        public BadRequestException(string message, string code) : base(message)
        {
            Code = code;
        }

        public IDictionary<string, string[]>? ValidationErrors { get; set; }

        /// <summary>Sent as the problem's "code" extension; most rejections have none.</summary>
        public string? Code { get; }
    }
}

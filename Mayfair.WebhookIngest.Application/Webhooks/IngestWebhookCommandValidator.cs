using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mayfair.WebhookIngest.Application.Webhooks
{
    public sealed class IngestWebhookCommandValidator : AbstractValidator<IngestWebhookCommand>
    {
        public IngestWebhookCommandValidator()
        {
            RuleFor(x => x.Provider).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Payload).NotEmpty();
        }
    }
}

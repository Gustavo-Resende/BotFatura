using FluentValidation;

namespace BotFatura.Application.Faturas.Commands.ConfigurarCobranca;

public class ConfigurarCobrancaCommandValidator : AbstractValidator<ConfigurarCobrancaCommand>
{
    public ConfigurarCobrancaCommandValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("O ID do cliente é obrigatório para registrar uma fatura.");

        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("O valor da fatura deve ser maior que zero.");

        RuleFor(x => x.DataVencimento)
            .NotEmpty().WithMessage("A data de vencimento da fatura é obrigatória.")
            .GreaterThan(DateTime.UtcNow.Date).WithMessage("A data de vencimento deve ser futura.");
    }
}

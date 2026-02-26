using FluentValidation;

namespace BotFatura.Application.Faturas.Commands.AtualizarFatura;

public class AtualizarFaturaCommandValidator : AbstractValidator<AtualizarFaturaCommand>
{
    public AtualizarFaturaCommandValidator()
    {
        RuleFor(c => c.FaturaId)
            .NotEmpty().WithMessage("O identificador da fatura é obrigatório.");

        RuleFor(c => c.Valor)
            .GreaterThan(0).WithMessage("O valor da fatura deve ser maior que zero.");

        RuleFor(c => c.DataVencimento)
            .NotEmpty().WithMessage("A data de vencimento é obrigatória.")
            .GreaterThan(DateTime.UtcNow.AddDays(-1))
            .WithMessage("A data de vencimento não pode ser no passado.");
    }
}

using FluentValidation;

namespace BotFatura.Application.Contratos.Commands.CriarContrato;

public class CriarContratoCommandValidator : AbstractValidator<CriarContratoCommand>
{
    public CriarContratoCommandValidator()
    {
        RuleFor(c => c.ClienteId)
            .NotEmpty().WithMessage("O cliente é obrigatório.");

        RuleFor(c => c.ValorMensal)
            .GreaterThan(0).WithMessage("O valor mensal deve ser maior que zero.");

        RuleFor(c => c.DiaVencimento)
            .InclusiveBetween(1, 28)
            .WithMessage("O dia de vencimento deve ser entre 1 e 28 para garantir compatibilidade com todos os meses do ano.");

        RuleFor(c => c.DataInicio)
            .NotEmpty().WithMessage("A data de início é obrigatória.");

        RuleFor(c => c.DataFim)
            .GreaterThan(c => c.DataInicio)
            .When(c => c.DataFim.HasValue)
            .WithMessage("A data de fim deve ser posterior à data de início.");
    }
}

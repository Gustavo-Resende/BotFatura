using FluentValidation;

namespace BotFatura.Application.Clientes.Commands.CadastrarCliente;

public class CadastrarClienteCommandValidator : AbstractValidator<CadastrarClienteCommand>
{
    public CadastrarClienteCommandValidator()
    {
        RuleFor(x => x.NomeCompleto)
            .NotEmpty().WithMessage("O nome completo do cliente é obrigatório.")
            .MinimumLength(3).WithMessage("Nome do cliente muito curto.");

        RuleFor(x => x.WhatsApp)
            .NotEmpty().WithMessage("O WhatsApp é obrigatório para notificações.")
            .Matches(@"^\+[1-9]\d{1,14}$").WithMessage("O número de WhatsApp deve seguir o padrão internacional (+55...).");
    }
}

using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.RegistrarPagamento;

public record RegistrarPagamentoCommand(Guid FaturaId) : IRequest<Result>;

namespace BotFatura.Application.Clientes.Queries.ObterClientePorId;

public record ClienteDto(Guid Id, string NomeCompleto, string WhatsApp, bool Ativo, DateTime CreatedAt);

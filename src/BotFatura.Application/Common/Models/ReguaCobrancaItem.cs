using BotFatura.Domain.Entities;

namespace BotFatura.Application.Common.Models;

public record ReguaCobrancaItem(Fatura Fatura, string TipoNotificacao);

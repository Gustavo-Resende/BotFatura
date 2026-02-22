using BotFatura.Application.Common.Models;
using MediatR;

namespace BotFatura.Application.Dashboard.Queries.ObterResumoDashboard;

public record ObterResumoDashboardQuery : IRequest<DashboardResumoDto>;

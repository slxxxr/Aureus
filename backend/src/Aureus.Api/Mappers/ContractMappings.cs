using Aureus.Domain.Workspaces;
using AutoMapper;
using Aureus.Api.Contracts.Analytics;
using Aureus.Api.Contracts.Categories;
using Aureus.Api.Contracts.FinancialAccounts;
using Aureus.Api.Contracts.Transactions;
using Aureus.Api.Contracts.Transfers;
using Aureus.Api.Contracts.Workspaces;
using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.Domain.Transactions;
using Aureus.Domain.Transfers;
using Aureus.Domain.Analytics;


namespace Aureus.Api.Mappers;

public sealed class ContractMappings : Profile
{
    public ContractMappings()
    {
        CreateMap<Category, CategoryResponse>();
        CreateMap<FinancialAccount, FinancialAccountResponse>();
        CreateMap<UserWorkspaceSummary, WorkspaceResponse>();
        CreateMap<WorkspaceInvitation, InvitationResponse>();
        CreateMap<PendingInvitationSummary, MyInvitationResponse>();
        CreateMap<Transaction, TransactionResponse>();
        CreateMap<Transfer, TransferResponse>();
        CreateMap<CurrencySummary, CurrencySummaryResponse>();
        CreateMap<BreakdownItem, BreakdownItemResponse>();
        CreateMap<TimeSeriesPoint, TimeSeriesPointResponse>();
        CreateMap<CategoryTimeSeriesPoint, CategoryTimeSeriesPointResponse>();
        CreateMap<WorkspaceMemberDetail, WorkspaceMemberResponse>();
    }
}

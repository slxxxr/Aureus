using AutoMapper;
using Aureus.Api.Contracts.Categories;
using Aureus.Api.Contracts.FinancialAccounts;
using Aureus.Api.Contracts.Workspaces;
using Aureus.Domain.Categories;
using Aureus.Domain.FinancialAccounts;
using Aureus.UseCases.Workspaces.GetUserWorkspaces;

namespace Aureus.Api.Mappers;

public sealed class ContractMappings : Profile
{
    public ContractMappings()
    {
        CreateMap<Category, CategoryResponse>();
        CreateMap<FinancialAccount, FinancialAccountResponse>();
        CreateMap<UserWorkspaceSummary, WorkspaceResponse>();
    }
}

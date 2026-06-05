using AutoMapper;
using Aureus.Domain.Users;
using Aureus.Domain.Workspaces;
using Aureus.Postgres.Entities;

namespace Aureus.Postgres.Mappers;

public sealed class DatabaseMappings : Profile
{
    public DatabaseMappings()
    {
        CreateMap<User, UserDb>();
        CreateMap<Workspace, WorkspaceDb>();
        CreateMap<WorkspaceMember, WorkspaceMemberDb>()
            .ForMember(
                destination => destination.Role,
                options => options.MapFrom(source => source.Role.ToString()));
    }
}

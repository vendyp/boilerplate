﻿using BoilerPlate.Shared.Abstraction.Commands;
using BoilerPlate.Shared.Abstraction.Primitives;

namespace BoilerPlate.Core.RoleManagement.Commands.EditRole;

public class EditRoleCommand : ICommand<Result>
{
    public Guid RoleId { get; set; }
    public string? Name { get; set; }
}
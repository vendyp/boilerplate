﻿using BoilerPlate.Core.Validators;

namespace BoilerPlate.Core.UserManagement.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(e => e.Username).NotEmpty().MinimumLength(4).MaximumLength(100);
        RuleFor(e => e.Password).SetValidator(new PasswordValidator());
        RuleFor(e => e.Fullname).NotEmpty().MinimumLength(4).MaximumLength(100);
    }
}
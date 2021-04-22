﻿using CanvasApi.Client.Modules.Enums;
using CanvasApi.Client.Role.Models;
using CanvasApi.Client.Users.Models;
using System.Collections.Generic;

namespace CanvasApi.Client.Modules.Models
{
    public interface IAdmin
    {
        long Id { get; }
        IRole Role { get; }
        IUser User { get; }
        IEnumerable<AdminListWorkflowState> WorkflowState { get; }
    }
}
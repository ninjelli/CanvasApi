﻿using System;
using System.Collections.Generic;
using CanvasApi.Client.Modules.Enums;
using CanvasApi.Client.Role.Models;
using CanvasApi.Client.Users.Models;
using Newtonsoft.Json;

namespace CanvasApi.Client.Modules.Models
{
    internal class Admin : IAdmin
    {
        [JsonProperty("id")] public long Id { get; set; }
        [JsonProperty("role")] public IRole Role { get; set; }
        IRole IAdmin.Role => this.Role;
        [JsonProperty("user")] public IUser User { get; set; }
        IUser IAdmin.User => this.User;
        [JsonProperty("workflow_state")] public IEnumerable<AdminListWorkflowState> WorkflowState { get; set; }
        IEnumerable<AdminListWorkflowState> IAdmin.WorkflowState => this.WorkflowState;
    }
}

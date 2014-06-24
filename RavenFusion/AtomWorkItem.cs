using System;

namespace RavenFusion.Models.AtomSide
{
    public class WorkItem
    {
        public int Id;
        public string Summary;
        public int WorkStatus;
        public int WorkItemType;
        public DateTime? ClosedDate;
        public DateTime? RequestedCompletionDate;
        public int AssignedTo_id; // Users/x
        public int ClosedBy_id; // Users/x
        public int Severity;
        public string CompletionComment;
        public string ImpactAnalysis;
        public int Department_id; // Enum - HandlingDepartment
        public DateTime? Rejected;
        public bool ClientRequirement;
        public int? EstimatedUnitOfWork;
        public int? ActualUnitsOfWork;
        public DateTime? EstimatedStartDate;
        public bool InHouseKeeping;
        public bool InternalTesting;
        public int ClosureReason_id; // Enum - ClosureReason
    }


    public class Comment
    {
        public string CommentText;
        public int UnitOfWork;
        public int Type;
        public int WorkItem_id;
    }


    public class WorkItemHistory
    {
        public int Id;
        public int WorkItem_Id;
    }

    public class WorkItemInsuranceCompany
    {
        public int Id;
        public int InsuranceCompany_id;
        public int WorkItem_id;
    }

    public class WorkItemProductGroupLink
    {
        public int Id;
        public int ProductGroup_id;
        public int WorkItem_id;
    }

    

    public class WorkItemSignOff
    {
        public int Id;
        public int SignOffType;
        public DateTime? SignedOff;
        public int WorkItem_id;
        public int SignedOffBy_id;
    }

    public class WorkItemSubscription
    {
        public int Id;
        public bool FollowUpdates;
        public int User_id;
        public int WorkItem_id;
        public int Event;
    }

    public class WorkItemSupplier
    {
        public int Id;
        public int Supplier_id;
        public int WorkItem_id;
    }

    public class UserViewCopy
    {
        public int id;
        public string UserID;
        public string Name;
        public int AccessLevel;
        public string team;
        public string EmailAddress;
        public int Department_id;
        public int PrimaryDepartment_id;
    }

    public class Profile
    {
        public int UserID;
        public int CurrentAvatar;
        public int CurrentSignature;
        public bool ShowFilters;
        public bool RefreshSearch;
        public bool IsAssignedToAuto;
        public string NickName;
        public string TrelloToken;
        public string SVNUsername;
    }

    public class ClosureReasonItem
    {
        public int Id;
        public string Description;
        public string Department;
        public bool Enabled;
    }

    public class HandlingDepartment
    {
        public int id;
        public string Email;
        public string Description;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Abstractions.Data;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace RavenFusion.Models.RavenSide
{
    public interface Item
    {

    }

    public class Comment
    {
        public string CommentText;
        public int Type;//Enum - CommentType
        public int UnitsOfWork;
    }

    public class WorkitemSignOff
    {
        public int SignOffType;
        public DateTime? SignedOff;
        public UserDenormalized SignedOffBy;//Users/x
    }
    
    public class WorkItem
    {
        public int? ActualUnitsOfWork;
        public UserDenormalized AssignedTo; // Users/x
        public bool ClientRequirement;
        public UserDenormalized ClosedBy; // Users/x
        public DateTime? ClosedDate;
        public ClosureReasonDenormalized ClosureReason; // Enum - ClosureReason
        public List<Comment> Comments;
        public string CompletionComment;
        public HandlingDepartmentDenormalized Department; // Enum - HandlingDepartment
        public List<Document> Documents; // Reference to Documents/x documents
        public DateTime? EstimatedStartDate;
        public int? EstimatedUnitOfWork;
        public List<int> History;
        public int Id;
        public string ImpactAnalysis;
        public List<int> InsuranceCompanies;
        public bool InternalTesting;
        public bool InHouseKeeping;
        public List<ProductGroupDenormalized> ProductGroups; // ProductGroup/x
        public DateTime? Rejected;
        public DateTime? RequestedCompletionDate;
        public int Severity;
        public List<WorkitemSignOff> SignOffs;
        public List<UserDenormalized> Subscriptions; //Users/x
        public string Summary;
        public List<int> Suppliers;
        public int WorkItemType;
        public int WorkStatus;
    }
    
    public class User
    {
        public int id;
        public string UserID;
        public string Name;
        public int? AccessLevel;
        public string team;
        public string EmailAddress;
        public int? Department_id;
        public int? PrimaryDepartment_id;
        public int? CurrentAvatar;
        public int? CurrentSignature;
        public bool? ShowFilters;
        public bool? RefreshSearch;
        public bool? IsAssignedToAuto;
        public string NickName;
        public string TrelloToken;
        public string SVNUsername;
    }

    public class ClosureReasonItem : Item
    {
        public string Id;
        public string Description;
        public string Department;
        public bool Enabled;
    }

    public class UserDenormalized
    {
        public int Id;
        public string Name;
        public string NickName;
    }

    public class ClosureReasonDenormalized
    {
        public string Id;
        public string Description;
    }

    public class HandlingDepartmentDenormalized
    {
        public string Description;
        public string Id;
    }

    public class ProductGroupDenormalized : Item
    {
        public int Id;
        public string Name;
    }
    
    public class HandlingDepartmentItem : Item
    {
        public string Email;
        public string Description;
        public string Id;
    }

    public class ProductGroupItem : Item
    {
        public int Id;
        public string Name;
        public int v2CatId;
        public int OrderBy;
    }
    
    public class CustomEnum
    {
        public List<Item> Items;
    }

    public class Document
    {
        public int Id;
        public int Revision;
        public string FileName;
    }

    public class FullTextQueryIndex : AbstractIndexCreationTask<WorkItem, FullTextQueryIndex.Result>
    {
        public class Result
        {
            public string Query;
        }

        public FullTextQueryIndex()
        {
            Map = workitems => from workitem in workitems
                select new
                {
                    Query = new object[]
                    {
                        workitem.AssignedTo.Name,
                        workitem.AssignedTo.NickName,
                        workitem.ClosedBy.Name,
                        workitem.ClosedBy.NickName,
                        workitem.Department.Description,
                        workitem.ProductGroups.Select(x => x.Name),
                        workitem.Subscriptions.Select(x => x.Name),
                        workitem.Subscriptions.Select(x => x.NickName),
                        workitem.Comments.Select(x => x.CommentText)
                    }
                };

            Index(x => x.Query, FieldIndexing.Analyzed);
        }
    }
}
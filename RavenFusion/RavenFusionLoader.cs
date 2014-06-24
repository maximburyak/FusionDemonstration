using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Raven.Abstractions.Indexing;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;
using RavenFusion.Models;
using RavenFusion.Models.AtomSide;
using RavenFusion.Models.RavenSide;
using ClosureReasonItem = RavenFusion.Models.AtomSide.ClosureReasonItem;
using Comment = RavenFusion.Models.AtomSide.Comment;
using WorkItem = RavenFusion.Models.AtomSide.WorkItem;
using System.Configuration;


namespace RavenFusion
{
    public class RavenFusionLoader
    {

        public void LoadSqlDataToRavenDB()
        {
            IEnumerable<RavenFusion.Models.AtomSide.WorkItem> atomWorkItems;
            IEnumerable<RavenFusion.Models.AtomSide.UserViewCopy> atomUserViewCopy;
            IEnumerable<RavenFusion.Models.AtomSide.Profile> atomProfile;
            List<RavenFusion.Models.RavenSide.WorkItem> workItemsToSave = new List<RavenFusion.Models.RavenSide.WorkItem>();
            string sqlConnString = ConfigurationManager.ConnectionStrings["FusionSqlServer"].ConnectionString;
            using (var connection = new SqlConnection(sqlConnString))
            {
                
                using (var store = new DocumentStore()
                {
                    ConnectionStringName = "RavenDBFusion"
                })
                {
                    connection.Open();
                    store.Initialize();

                    var allUsers = LoadUsers(connection, store);
                    var allReasons = LoadClosureReasons(connection, store);
                    var allHandlingDepartments = LoadHandlingDepartments(connection, store);
                    var allProductGroups = LoadProductGroups(connection, store);

                    LoadWorkItems(store, connection, allReasons, allHandlingDepartments, allProductGroups, allUsers);

                    GenerateIndexes(store);
                }
            }
        }

        private void GenerateIndexes(DocumentStore store)
        {
            new FullTextQueryIndex().Execute(store);
        }

        private void LoadWorkItems(DocumentStore store, SqlConnection connection, IEnumerable<ClosureReasonItem> allReasons,
            IEnumerable<HandlingDepartment> allHandlingDepartments, IEnumerable<ProductGroupItem> allProductGroups, IEnumerable<User> allUsers)
        {
            IEnumerable<WorkItem> atomWorkItems;
            // load workItems
            using (var worekeiItemsBulkInsert = store.BulkInsert())
            {
                atomWorkItems =
                    connection.Query<RavenFusion.Models.AtomSide.WorkItem>(@"SELECT * FROM WORKITEM ORDER BY ID DESC");

                var closureReasonDictionary = allReasons.ToDictionary(closureReason => closureReason.Id);
                var handlingDepartmentsDictionary = allHandlingDepartments.ToDictionary(x => x.id);
                var productGroupsDictionary = allProductGroups.ToDictionary(x => x.Id);
                var usersDictionary = allUsers.ToDictionary(x => x.id);

                foreach (var curWorkItem in atomWorkItems)
                {
                    GenerateWorkItem(connection, curWorkItem, closureReasonDictionary, handlingDepartmentsDictionary, productGroupsDictionary, worekeiItemsBulkInsert, usersDictionary);
                }
                
            }
        }

        private void GenerateWorkItem(SqlConnection connection, WorkItem curWorkItem, Dictionary<int, ClosureReasonItem> closureReasonDictionary,
            Dictionary<int, HandlingDepartment> handlingDepartmentsDictionary, Dictionary<int, ProductGroupItem> productGroupsDictionary, BulkInsertOperation workItemsSession,
            Dictionary<int, User> usersDictionary)
        {
            var comments =
                connection.Query<RavenFusion.Models.AtomSide.Comment>(@"SELECT * FROM COMMENT WHERE WORKITEM_ID=@ID",
                    new { ID = curWorkItem.Id });
            var history =
                connection.Query<RavenFusion.Models.AtomSide.WorkItemHistory>(
                    @"SELECT * FROM WORKITEMHISTORY WHERE WORKITEM_ID=@ID", new { ID = curWorkItem.Id });
            var insuraceCompanies =
                connection.Query<RavenFusion.Models.AtomSide.WorkItemInsuranceCompany>(
                    @"SELECT * FROM WorkItemInsuranceCompany WHERE WORKITEM_ID=@ID", new { ID = curWorkItem.Id });

            var workItemSignOff =
                connection.Query<RavenFusion.Models.AtomSide.WorkItemSignOff>(
                    @"SELECT * FROM WORKITEMSIGNOFF WHERE WORKITEM_ID=@ID", new { ID = curWorkItem.Id });
            var workItemSubscription =
                connection.Query<RavenFusion.Models.AtomSide.WorkItemSubscription>(
                    @"SELECT * FROM SUBSCRIPTION WHERE WORKITEM_ID=@ID", new { ID = curWorkItem.Id });
            var workItemSupplier =
                connection.Query<RavenFusion.Models.AtomSide.WorkItemSupplier>(
                    @"SELECT * FROM WORKITEMSUPPLIER WHERE WORKITEM_ID=@ID", new { ID = curWorkItem.Id });
            var workItemDocuments = connection.Query<RavenFusion.Models.RavenSide.Document>(
                @"SELECT * FROM DOCUMENT WHERE WORKITEM_ID=@ID", new { ID = curWorkItem.Id });
            RavenFusion.Models.AtomSide.ClosureReasonItem workItemClosureReason = null;
            closureReasonDictionary.TryGetValue(curWorkItem.ClosureReason_id, out workItemClosureReason);

            RavenFusion.Models.AtomSide.HandlingDepartment workItemHandlingDepartment = null;
            handlingDepartmentsDictionary.TryGetValue(curWorkItem.Department_id,
                out workItemHandlingDepartment);

            var workItemProductGroups = new List<RavenFusion.Models.RavenSide.ProductGroupDenormalized>();

            foreach (var productGroupLink in connection.Query<RavenFusion.Models.AtomSide.WorkItemProductGroupLink>(
                @"SELECT * FROM WorkItemProductGroup WHERE WORKITEM_ID=@ID",
                new { ID = curWorkItem.Id }))
            {
                RavenFusion.Models.RavenSide.ProductGroupItem curProductGroup = null;
                if (productGroupsDictionary.TryGetValue(productGroupLink.ProductGroup_id, out curProductGroup))
                {
                    workItemProductGroups.Add(new RavenFusion.Models.RavenSide.ProductGroupDenormalized()
                    {
                        Id = curProductGroup.Id,
                        Name = curProductGroup.Name
                    });
                }
            }


            workItemsSession.Store(
                new RavenFusion.Models.RavenSide.WorkItem()
                {
                    ActualUnitsOfWork = curWorkItem.ActualUnitsOfWork,
                    AssignedTo =
                        usersDictionary.ContainsKey(curWorkItem.AssignedTo_id)
                            ? new RavenFusion.Models.RavenSide.UserDenormalized()
                            {
                                Id = usersDictionary[curWorkItem.AssignedTo_id].id,
                                Name = usersDictionary[curWorkItem.AssignedTo_id].Name,
                                NickName = usersDictionary[curWorkItem.AssignedTo_id].NickName
                            }
                            : null,
                    ClientRequirement = curWorkItem.ClientRequirement,
                    ClosedBy =
                        usersDictionary.ContainsKey(curWorkItem.ClosedBy_id)
                            ? new RavenFusion.Models.RavenSide.UserDenormalized()
                            {
                                Id = usersDictionary[curWorkItem.ClosedBy_id].id,
                                Name = usersDictionary[curWorkItem.ClosedBy_id].Name,
                                NickName = usersDictionary[curWorkItem.ClosedBy_id].NickName
                            }
                            : null,
                    ClosedDate = curWorkItem.ClosedDate,
                    ClosureReason =
                        workItemClosureReason != null
                            ? new RavenFusion.Models.RavenSide.ClosureReasonDenormalized()
                            {
                                Description = workItemClosureReason.Description,
                                Id = workItemClosureReason.Id.ToString()
                            }
                            : null,
                    Comments = comments.Select(x => new RavenFusion.Models.RavenSide.Comment()
                    {
                        CommentText = x.CommentText,
                        Type = x.Type,
                        UnitsOfWork = x.UnitOfWork
                    }).ToList(),
                    CompletionComment = curWorkItem.CompletionComment,
                    Department = workItemHandlingDepartment != null
                        ? new RavenFusion.Models.RavenSide.HandlingDepartmentDenormalized()
                        {
                            Description = workItemHandlingDepartment.Description,
                            Id = workItemHandlingDepartment.id.ToString()
                        }
                        : null,
                    EstimatedStartDate = curWorkItem.EstimatedStartDate,
                    EstimatedUnitOfWork = curWorkItem.EstimatedUnitOfWork,
                    History = history.Select(x => x.Id).ToList(),
                    ImpactAnalysis = curWorkItem.ImpactAnalysis,
                    InHouseKeeping = curWorkItem.InHouseKeeping,
                    InsuranceCompanies = insuraceCompanies.Select(x => x.InsuranceCompany_id).ToList(),
                    InternalTesting = curWorkItem.InternalTesting,
                    ProductGroups = workItemProductGroups.Count > 0 ? workItemProductGroups : null,
                    Rejected = curWorkItem.Rejected,
                    RequestedCompletionDate = curWorkItem.RequestedCompletionDate,
                    Severity = curWorkItem.Severity,
                    SignOffs = workItemSignOff.Select(x => new RavenFusion.Models.RavenSide.WorkitemSignOff()
                    {
                        SignOffType = x.SignOffType,
                        SignedOff = x.SignedOff,
                        SignedOffBy =
                            usersDictionary.ContainsKey(x.SignedOffBy_id)
                                ? new RavenFusion.Models.RavenSide.UserDenormalized()
                                {
                                    Id = usersDictionary[x.SignedOffBy_id].id,
                                    Name = usersDictionary[x.SignedOffBy_id].Name,
                                    NickName = usersDictionary[x.SignedOffBy_id].NickName
                                }
                                : null,
                    }).Where(x => x.SignedOffBy != null).ToList(),
                    Subscriptions =
                        workItemSubscription.Select(
                            x =>
                                usersDictionary.ContainsKey(x.User_id)
                                    ? new RavenFusion.Models.RavenSide.UserDenormalized()
                                    {
                                        Id = usersDictionary[x.User_id].id,
                                        Name = usersDictionary[x.User_id].Name,
                                        NickName = usersDictionary[x.User_id].NickName
                                    }
                                    : null).Where(x => x != null).ToList(),
                    Summary = curWorkItem.Summary,
                    Suppliers = workItemSupplier.Select(x => x.Supplier_id).ToList(),
                    WorkItemType = curWorkItem.WorkItemType,
                    WorkStatus = curWorkItem.WorkStatus,
                    Documents = workItemDocuments.ToList()
                }, @"WorkItems/" + curWorkItem.Id);
        }

        private IEnumerable<User> LoadUsers(SqlConnection connection, DocumentStore store)
        {
            // load users
            var allUsers = connection.Query<RavenFusion.Models.RavenSide.User>(
                @"SELECT A.ID,A.USERID,A.NAME,A.ACCESSLEVEL,A.TEAM,A.EMAILADDRESS,A.DEPARTMENT_ID,A.PRIMARYDEPARTMENT_ID," +
                "B.CurrentAvatar, B.CurrentSignature, B.ShowFilters, B.REFRESHSEARCH, B.ISASSIGNEDTOAUTO, B.NICKNAME,B.TRELLOTOKEN,B.SVNUSERNAME " +
                "FROM USERVIEWCOPY A, PROFILE B WHERE B.USERID = A.ID");
            using (var usersSession = store.OpenSession())
            {
                foreach (var user in allUsers)
                {
                    usersSession.Store(user, @"Users/" + user.id);
                }
                usersSession.SaveChanges();
            }
            return allUsers;
        }

        private IEnumerable<ClosureReasonItem> LoadClosureReasons(SqlConnection connection, DocumentStore store)
        {
            // load Closure reasons
            var allReasons = connection.Query<RavenFusion.Models.AtomSide.ClosureReasonItem>(@"SELECT * FROM CLOSUREREASON");
            using (var closureReasonsSession = store.OpenSession())
            {
                closureReasonsSession.Store(new RavenFusion.Models.RavenSide.CustomEnum
                {
                    Items = allReasons.Select(x => new RavenFusion.Models.RavenSide.ClosureReasonItem()
                    {
                        Department = x.Department,
                        Description = x.Description,
                        Enabled = x.Enabled,
                        Id = x.Id.ToString()
                    }).ToList<RavenFusion.Models.RavenSide.Item>()
                }, "Enums/ClosureReasons");
                closureReasonsSession.SaveChanges();
            }
            return allReasons;
        }

        private IEnumerable<HandlingDepartment> LoadHandlingDepartments(SqlConnection connection, DocumentStore store)
        {
            // load Handling Departments
            var allHandlingDepartments =
                connection.Query<RavenFusion.Models.AtomSide.HandlingDepartment>(@"SELECT * FROM HANDLINGDEPARTMENT");
            using (var handlingDepartmentsSession = store.OpenSession())
            {
                handlingDepartmentsSession.Store(new RavenFusion.Models.RavenSide.CustomEnum
                {
                    Items = allHandlingDepartments.Select(x => new RavenFusion.Models.RavenSide.HandlingDepartmentItem()
                    {
                        Description = x.Description,
                        Email = x.Email,
                        Id = x.id.ToString()
                    }).ToList<RavenFusion.Models.RavenSide.Item>()
                }, "Enums/HandlingDepartments");
                handlingDepartmentsSession.SaveChanges();
            }
            return allHandlingDepartments;
        }

        private IEnumerable<ProductGroupItem> LoadProductGroups(SqlConnection connection, DocumentStore store)
        {
            // load product groups
            var allProductGroups = connection.Query<RavenFusion.Models.RavenSide.ProductGroupItem>(@"SELECT * FROM PRODUCTGROUP");
            using (var productGroupSession = store.OpenSession())
            {
                productGroupSession.Store(new RavenFusion.Models.RavenSide.CustomEnum
                {
                    Items = allProductGroups.ToList<RavenFusion.Models.RavenSide.Item>()
                }, "Enums/ProductGroups");
                productGroupSession.SaveChanges();
            }
            return allProductGroups;
        }


    }


}

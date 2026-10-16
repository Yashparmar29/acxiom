using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Security;

namespace AcxiomCRM.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Apply migrations automatically
        await context.Database.MigrateAsync();

        // 1. Seed Roles
        foreach (var role in CrmRoles.AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Users
        var usersToSeed = new[]
        {
            new { Email = "admin@acxiomcrm.com", Name = "System Administrator", Role = CrmRoles.Admin, Pass = "Admin@12345", Phone = "9820011223" },
            new { Email = "manager@acxiomcrm.com", Name = "Sales Manager", Role = CrmRoles.Manager, Pass = "Manager@12345", Phone = "9820022334" },
            new { Email = "sales1@acxiomcrm.com", Name = "Vikram Sharma (Sales)", Role = CrmRoles.SalesExecutive, Pass = "Sales@12345", Phone = "9820033445" },
            new { Email = "sales2@acxiomcrm.com", Name = "Ananya Patel (Sales)", Role = CrmRoles.SalesExecutive, Pass = "Sales@12345", Phone = "9820044556" }
        };

        var seededUsers = new Dictionary<string, ApplicationUser>();

        foreach (var u in usersToSeed)
        {
            var user = await userManager.FindByEmailAsync(u.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = u.Email,
                    Email = u.Email,
                    FullName = u.Name,
                    PhoneNumber = u.Phone,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-6)
                };

                var res = await userManager.CreateAsync(user, u.Pass);
                if (res.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, u.Role);
                }
            }
            seededUsers[u.Email] = user;
        }

        var sales1 = seededUsers["sales1@acxiomcrm.com"];
        var sales2 = seededUsers["sales2@acxiomcrm.com"];
        var manager = seededUsers["manager@acxiomcrm.com"];
        var admin = seededUsers["admin@acxiomcrm.com"];

        // 3. Seed Customers (15 customers)
        if (!await context.Customers.AnyAsync())
        {
            var customers = new List<Customer>
            {
                new() { CustomerCode = "CUST-0001", CustomerName = "Reliance Digital Solutions", Email = "contact@reliancedigi.com", Phone = "9811001101", CompanyName = "Reliance Group", City = "Mumbai", State = "Maharashtra", Status = CustomerStatus.Active, AssignedTo = sales1.Id, CreatedBy = admin.Id, CreatedDate = DateTime.UtcNow.AddMonths(-5) },
                new() { CustomerCode = "CUST-0002", CustomerName = "Tata Consultancy Services", Email = "procurement@tcs-india.com", Phone = "9811001102", CompanyName = "Tata Sons", City = "Mumbai", State = "Maharashtra", Status = CustomerStatus.Active, AssignedTo = sales1.Id, CreatedBy = admin.Id, CreatedDate = DateTime.UtcNow.AddMonths(-5) },
                new() { CustomerCode = "CUST-0003", CustomerName = "Infosys Technologies", Email = "vendors@infosys-tech.com", Phone = "9811001103", CompanyName = "Infosys Ltd", City = "Bengaluru", State = "Karnataka", Status = CustomerStatus.Active, AssignedTo = sales2.Id, CreatedBy = manager.Id, CreatedDate = DateTime.UtcNow.AddMonths(-4) },
                new() { CustomerCode = "CUST-0004", CustomerName = "HDFC Bank Corporate", Email = "it.desk@hdfcbank-corp.com", Phone = "9811001104", CompanyName = "HDFC Bank", City = "Mumbai", State = "Maharashtra", Status = CustomerStatus.Active, AssignedTo = sales2.Id, CreatedBy = manager.Id, CreatedDate = DateTime.UtcNow.AddMonths(-4) },
                new() { CustomerCode = "CUST-0005", CustomerName = "Wipro Enterprise Services", Email = "crm.support@wipro-ent.com", Phone = "9811001105", CompanyName = "Wipro Ltd", City = "Bengaluru", State = "Karnataka", Status = CustomerStatus.Active, AssignedTo = sales1.Id, CreatedBy = admin.Id, CreatedDate = DateTime.UtcNow.AddMonths(-4) },
                new() { CustomerCode = "CUST-0006", CustomerName = "Bharti Airtel Telecom", Email = "enterprise@airtel-telecom.com", Phone = "9811001106", CompanyName = "Bharti Enterprises", City = "New Delhi", State = "Delhi", Status = CustomerStatus.Active, AssignedTo = sales2.Id, CreatedBy = admin.Id, CreatedDate = DateTime.UtcNow.AddMonths(-3) },
                new() { CustomerCode = "CUST-0007", CustomerName = "Adani Logistics Ltd", Email = "ops@adani-logistics.com", Phone = "9811001107", CompanyName = "Adani Ports", City = "Ahmedabad", State = "Gujarat", Status = CustomerStatus.Active, AssignedTo = sales1.Id, CreatedBy = manager.Id, CreatedDate = DateTime.UtcNow.AddMonths(-3) },
                new() { CustomerCode = "CUST-0008", CustomerName = "Mahindra & Mahindra Tech", Email = "it@mahindra-tech.com", Phone = "9811001108", CompanyName = "Mahindra Group", City = "Pune", State = "Maharashtra", Status = CustomerStatus.Active, AssignedTo = sales2.Id, CreatedBy = manager.Id, CreatedDate = DateTime.UtcNow.AddMonths(-3) },
                new() { CustomerCode = "CUST-0009", CustomerName = "ICICI Prudential Life", Email = "corp@icicipru-life.com", Phone = "9811001109", CompanyName = "ICICI Group", City = "Mumbai", State = "Maharashtra", Status = CustomerStatus.Active, AssignedTo = sales1.Id, CreatedBy = admin.Id, CreatedDate = DateTime.UtcNow.AddMonths(-2) },
                new() { CustomerCode = "CUST-0010", CustomerName = "Bajaj Finserv Digital", Email = "contact@bajajfinserv-digi.com", Phone = "9811001110", CompanyName = "Bajaj Group", City = "Pune", State = "Maharashtra", Status = CustomerStatus.Active, AssignedTo = sales2.Id, CreatedBy = manager.Id, CreatedDate = DateTime.UtcNow.AddMonths(-2) },
                new() { CustomerCode = "CUST-0011", CustomerName = "Sun Pharmaceutical Industries", Email = "supply@sunpharma-ind.com", Phone = "9811001111", CompanyName = "Sun Pharma", City = "Vadodara", State = "Gujarat", Status = CustomerStatus.Active, AssignedTo = sales1.Id, CreatedBy = admin.Id, CreatedDate = DateTime.UtcNow.AddMonths(-2) },
                new() { CustomerCode = "CUST-0012", CustomerName = "Larsen & Toubro Infotech", Email = "cloud@lnt-infotech.com", Phone = "9811001112", CompanyName = "L&T Group", City = "Mumbai", State = "Maharashtra", Status = CustomerStatus.Active, AssignedTo = sales2.Id, CreatedBy = manager.Id, CreatedDate = DateTime.UtcNow.AddMonths(-1) },
                new() { CustomerCode = "CUST-0013", CustomerName = "Kotak Mahindra Capital", Email = "dealdesk@kotak-cap.com", Phone = "9811001113", CompanyName = "Kotak Group", City = "Mumbai", State = "Maharashtra", Status = CustomerStatus.Active, AssignedTo = sales1.Id, CreatedBy = sales1.Id, CreatedDate = DateTime.UtcNow.AddMonths(-1) },
                new() { CustomerCode = "CUST-0014", CustomerName = "Zomato Logistics Tech", Email = "partners@zomato-logi.com", Phone = "9811001114", CompanyName = "Zomato Ltd", City = "Gurugram", State = "Haryana", Status = CustomerStatus.Active, AssignedTo = sales2.Id, CreatedBy = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-15) },
                new() { CustomerCode = "CUST-0015", CustomerName = "Paytm Financial Cloud", Email = "merchants@paytm-cloud.com", Phone = "9811001115", CompanyName = "One97 Comm", City = "Noida", State = "Uttar Pradesh", Status = CustomerStatus.Inactive, AssignedTo = sales1.Id, CreatedBy = admin.Id, CreatedDate = DateTime.UtcNow.AddDays(-10) }
            };

            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();
        }

        var customerList = await context.Customers.ToListAsync();

        // 4. Seed Leads (20 leads covering all statuses)
        if (!await context.Leads.AnyAsync())
        {
            var leads = new List<Lead>
            {
                new() { LeadCode = "LEAD-0001", LeadName = "Rajesh Gupta", Email = "rajesh.gupta@delhicorp.in", Phone = "9822001101", CompanyName = "Delhi Corp", Source = LeadSource.Website, Status = LeadStatus.New, Priority = LeadPriority.High, ExpectedValue = 450000m, AssignedTo = sales1.Id, CreatedBy = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-2) },
                new() { LeadCode = "LEAD-0002", LeadName = "Sneha Kulkarni", Email = "sneha.k@puneventures.in", Phone = "9822001102", CompanyName = "Pune Ventures", Source = LeadSource.Referral, Status = LeadStatus.Contacted, Priority = LeadPriority.Medium, ExpectedValue = 300000m, AssignedTo = sales1.Id, CreatedBy = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-5) },
                new() { LeadCode = "LEAD-0003", LeadName = "Arjun Nambiar", Email = "arjun.n@kochitech.com", Phone = "9822001103", CompanyName = "Kochi Tech", Source = LeadSource.SocialMedia, Status = LeadStatus.Qualified, Priority = LeadPriority.High, ExpectedValue = 750000m, AssignedTo = sales1.Id, CreatedBy = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-8) },
                new() { LeadCode = "LEAD-0004", LeadName = "Pooja Hegde", Email = "pooja@mangaloreexports.in", Phone = "9822001104", CompanyName = "Mangalore Exports", Source = LeadSource.ColdCall, Status = LeadStatus.Unqualified, Priority = LeadPriority.Low, ExpectedValue = 120000m, AssignedTo = sales1.Id, CreatedBy = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-12) },
                new() { LeadCode = "LEAD-0005", LeadName = "Reliance Expansion Team", Email = "contact@reliancedigi.com", Phone = "9811001101", CompanyName = "Reliance Group", Source = LeadSource.Referral, Status = LeadStatus.Converted, Priority = LeadPriority.High, ExpectedValue = 1200000m, ConvertedCustomerId = customerList[0].CustomerId, ConvertedDate = DateTime.UtcNow.AddMonths(-4), AssignedTo = sales1.Id, CreatedBy = sales1.Id, CreatedDate = DateTime.UtcNow.AddMonths(-5) },
                new() { LeadCode = "LEAD-0006", LeadName = "Kunal Shah", Email = "kunal@creditinnovations.in", Phone = "9822001106", CompanyName = "Credit Innovations", Source = LeadSource.Event, Status = LeadStatus.Lost, Priority = LeadPriority.Medium, ExpectedValue = 500000m, AssignedTo = sales1.Id, CreatedBy = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-20) },

                new() { LeadCode = "LEAD-0007", LeadName = "Meera Sen", Email = "meera.sen@kolkatait.com", Phone = "9822001107", CompanyName = "Kolkata IT Solutions", Source = LeadSource.EmailCampaign, Status = LeadStatus.New, Priority = LeadPriority.Low, ExpectedValue = 250000m, AssignedTo = sales2.Id, CreatedBy = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-3) },
                new() { LeadCode = "LEAD-0008", LeadName = "Deepak Verma", Email = "deepak@jaipurjewels.com", Phone = "9822001108", CompanyName = "Jaipur Gems", Source = LeadSource.Website, Status = LeadStatus.Contacted, Priority = LeadPriority.High, ExpectedValue = 600000m, AssignedTo = sales2.Id, CreatedBy = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-6) },
                new() { LeadCode = "LEAD-0009", LeadName = "Sunita Rao", Email = "sunita@hydpharma.com", Phone = "9822001109", CompanyName = "Hyderabad Bio", Source = LeadSource.Event, Status = LeadStatus.Qualified, Priority = LeadPriority.High, ExpectedValue = 900000m, AssignedTo = sales2.Id, CreatedBy = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-10) },
                new() { LeadCode = "LEAD-0010", LeadName = "Manish Tiwari", Email = "manish@lucknowdist.com", Phone = "9822001110", CompanyName = "Lucknow Dist", Source = LeadSource.ColdCall, Status = LeadStatus.Unqualified, Priority = LeadPriority.Low, ExpectedValue = 90000m, AssignedTo = sales2.Id, CreatedBy = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-15) },
                new() { LeadCode = "LEAD-0011", LeadName = "Tata Cloud Migration", Email = "procurement@tcs-india.com", Phone = "9811001102", CompanyName = "Tata Sons", Source = LeadSource.Website, Status = LeadStatus.Converted, Priority = LeadPriority.High, ExpectedValue = 2500000m, ConvertedCustomerId = customerList[1].CustomerId, ConvertedDate = DateTime.UtcNow.AddMonths(-3), AssignedTo = sales2.Id, CreatedBy = sales2.Id, CreatedDate = DateTime.UtcNow.AddMonths(-4) },
                new() { LeadCode = "LEAD-0012", LeadName = "Vikas Oberoi", Email = "vikas@mumbaiconstruct.com", Phone = "9822001112", CompanyName = "Mumbai Realty", Source = LeadSource.Other, Status = LeadStatus.Lost, Priority = LeadPriority.Medium, ExpectedValue = 400000m, AssignedTo = sales2.Id, CreatedBy = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-25) },

                new() { LeadCode = "LEAD-0013", LeadName = "Ravi Teja", Email = "ravi@vizagsteels.in", Phone = "9822001113", CompanyName = "Vizag Steels", Source = LeadSource.Website, Status = LeadStatus.New, Priority = LeadPriority.Medium, ExpectedValue = 350000m, AssignedTo = sales1.Id, CreatedBy = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-1) },
                new() { LeadCode = "LEAD-0014", LeadName = "Geeta Goswami", Email = "geeta@assamteas.in", Phone = "9822001114", CompanyName = "Assam Organics", Source = LeadSource.Referral, Status = LeadStatus.Contacted, Priority = LeadPriority.High, ExpectedValue = 550000m, AssignedTo = sales1.Id, CreatedBy = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-4) },
                new() { LeadCode = "LEAD-0015", LeadName = "Sanjay Singhal", Email = "sanjay@chandigarhfab.com", Phone = "9822001115", CompanyName = "Chandigarh Fab", Source = LeadSource.SocialMedia, Status = LeadStatus.Qualified, Priority = LeadPriority.Medium, ExpectedValue = 420000m, AssignedTo = sales1.Id, CreatedBy = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-7) },
                new() { LeadCode = "LEAD-0016", LeadName = "Kavita Reddy", Email = "kavita@chennailogistics.in", Phone = "9822001116", CompanyName = "Chennai Logistics", Source = LeadSource.Website, Status = LeadStatus.New, Priority = LeadPriority.High, ExpectedValue = 800000m, AssignedTo = sales2.Id, CreatedBy = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-1) },
                new() { LeadCode = "LEAD-0017", LeadName = "Naveen Jindal", Email = "naveen@bhopalparts.com", Phone = "9822001117", CompanyName = "Bhopal Auto", Source = LeadSource.Event, Status = LeadStatus.Contacted, Priority = LeadPriority.Medium, ExpectedValue = 310000m, AssignedTo = sales2.Id, CreatedBy = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-5) },
                new() { LeadCode = "LEAD-0018", LeadName = "Rashmi Deshmukh", Email = "rashmi@nagpuragro.com", Phone = "9822001118", CompanyName = "Nagpur Agro", Source = LeadSource.Referral, Status = LeadStatus.Qualified, Priority = LeadPriority.High, ExpectedValue = 680000m, AssignedTo = sales2.Id, CreatedBy = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-8) },
                new() { LeadCode = "LEAD-0019", LeadName = "Amitabh Bose", Email = "amitabh@ranchiminers.in", Phone = "9822001119", CompanyName = "Ranchi Mining", Source = LeadSource.ColdCall, Status = LeadStatus.Lost, Priority = LeadPriority.Low, ExpectedValue = 150000m, AssignedTo = sales1.Id, CreatedBy = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-30) },
                new() { LeadCode = "LEAD-0020", LeadName = "Preeti Nair", Email = "preeti@coimbatoretextiles.com", Phone = "9822001120", CompanyName = "Coimbatore Mills", Source = LeadSource.EmailCampaign, Status = LeadStatus.New, Priority = LeadPriority.Medium, ExpectedValue = 380000m, AssignedTo = sales2.Id, CreatedBy = sales2.Id, CreatedDate = DateTime.UtcNow }
            };

            context.Leads.AddRange(leads);
            await context.SaveChangesAsync();
        }

        // 5. Seed Opportunities (15 opportunities across all stages)
        if (!await context.Opportunities.AnyAsync())
        {
            var opps = new List<Opportunity>
            {
                new() { OpportunityName = "Reliance Enterprise Cloud Suite", CustomerId = customerList[0].CustomerId, Amount = 1200000m, Stage = OpportunityStage.Won, Probability = 100, ExpectedCloseDate = DateTime.UtcNow.AddMonths(-3), Status = OpportunityStatus.Won, Source = "Referral", AssignedTo = sales1.Id, CreatedDate = DateTime.UtcNow.AddMonths(-4) },
                new() { OpportunityName = "TCS Global AI Infrastructure", CustomerId = customerList[1].CustomerId, Amount = 2500000m, Stage = OpportunityStage.Won, Probability = 100, ExpectedCloseDate = DateTime.UtcNow.AddMonths(-2), Status = OpportunityStatus.Won, Source = "Direct", AssignedTo = sales1.Id, CreatedDate = DateTime.UtcNow.AddMonths(-3) },
                new() { OpportunityName = "Infosys Security Gateway 2026", CustomerId = customerList[2].CustomerId, Amount = 850000m, Stage = OpportunityStage.Won, Probability = 100, ExpectedCloseDate = DateTime.UtcNow.AddMonths(-1), Status = OpportunityStatus.Won, Source = "Website", AssignedTo = sales2.Id, CreatedDate = DateTime.UtcNow.AddMonths(-2) },
                new() { OpportunityName = "HDFC Core Banking Integration", CustomerId = customerList[3].CustomerId, Amount = 1800000m, Stage = OpportunityStage.Negotiation, Probability = 75, ExpectedCloseDate = DateTime.UtcNow.AddDays(20), Status = OpportunityStatus.Open, Source = "Partner", AssignedTo = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-30) },
                new() { OpportunityName = "Wipro DevOps Platform License", CustomerId = customerList[4].CustomerId, Amount = 650000m, Stage = OpportunityStage.Proposal, Probability = 40, ExpectedCloseDate = DateTime.UtcNow.AddDays(25), Status = OpportunityStatus.Open, Source = "Referral", AssignedTo = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-20) },
                new() { OpportunityName = "Airtel 5G Billing Engine", CustomerId = customerList[5].CustomerId, Amount = 3200000m, Stage = OpportunityStage.Qualification, Probability = 10, ExpectedCloseDate = DateTime.UtcNow.AddDays(45), Status = OpportunityStatus.Open, Source = "Tender", AssignedTo = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-10) },
                new() { OpportunityName = "Adani Port Tracking System", CustomerId = customerList[6].CustomerId, Amount = 950000m, Stage = OpportunityStage.Negotiation, Probability = 75, ExpectedCloseDate = DateTime.UtcNow.AddDays(15), Status = OpportunityStatus.Open, Source = "Website", AssignedTo = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-25) },
                new() { OpportunityName = "Mahindra Fleet IoT Solution", CustomerId = customerList[7].CustomerId, Amount = 1100000m, Stage = OpportunityStage.Proposal, Probability = 40, ExpectedCloseDate = DateTime.UtcNow.AddDays(35), Status = OpportunityStatus.Open, Source = "Event", AssignedTo = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-18) },
                new() { OpportunityName = "ICICI Policy CRM Enhancement", CustomerId = customerList[8].CustomerId, Amount = 750000m, Stage = OpportunityStage.Qualification, Probability = 10, ExpectedCloseDate = DateTime.UtcNow.AddDays(50), Status = OpportunityStatus.Open, Source = "Direct", AssignedTo = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-5) },
                new() { OpportunityName = "Bajaj Consumer Lending Portal", CustomerId = customerList[9].CustomerId, Amount = 1400000m, Stage = OpportunityStage.Won, Probability = 100, ExpectedCloseDate = DateTime.UtcNow.AddDays(-5), Status = OpportunityStatus.Won, Source = "Referral", AssignedTo = sales2.Id, CreatedDate = DateTime.UtcNow.AddMonths(-1) },
                new() { OpportunityName = "Sun Pharma Batch Analytics", CustomerId = customerList[10].CustomerId, Amount = 500000m, Stage = OpportunityStage.Lost, Probability = 0, ExpectedCloseDate = DateTime.UtcNow.AddDays(-15), Status = OpportunityStatus.Lost, Source = "Cold Call", AssignedTo = sales1.Id, CreatedDate = DateTime.UtcNow.AddMonths(-1) },
                new() { OpportunityName = "L&T Defense ERP Addon", CustomerId = customerList[11].CustomerId, Amount = 2200000m, Stage = OpportunityStage.Negotiation, Probability = 75, ExpectedCloseDate = DateTime.UtcNow.AddDays(30), Status = OpportunityStatus.Open, Source = "Partner", AssignedTo = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-14) },
                new() { OpportunityName = "Kotak Trading Middleware", CustomerId = customerList[12].CustomerId, Amount = 900000m, Stage = OpportunityStage.Proposal, Probability = 40, ExpectedCloseDate = DateTime.UtcNow.AddDays(40), Status = OpportunityStatus.Open, Source = "Website", AssignedTo = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-12) },
                new() { OpportunityName = "Zomato Kitchen Inventory API", CustomerId = customerList[13].CustomerId, Amount = 400000m, Stage = OpportunityStage.Qualification, Probability = 10, ExpectedCloseDate = DateTime.UtcNow.AddDays(60), Status = OpportunityStatus.Open, Source = "Event", AssignedTo = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-3) },
                new() { OpportunityName = "Paytm Legacy Gateway Support", CustomerId = customerList[14].CustomerId, Amount = 300000m, Stage = OpportunityStage.Lost, Probability = 0, ExpectedCloseDate = DateTime.UtcNow.AddDays(-20), Status = OpportunityStatus.Lost, Source = "Email", AssignedTo = sales1.Id, CreatedDate = DateTime.UtcNow.AddDays(-40) }
            };

            context.Opportunities.AddRange(opps);
            await context.SaveChangesAsync();
        }

        var oppList = await context.Opportunities.ToListAsync();

        // 6. Seed Follow-Ups (15 follow-ups: past completed/missed, today, upcoming)
        if (!await context.FollowUps.AnyAsync())
        {
            var followUps = new List<FollowUp>
            {
                // Past completed
                new() { CustomerId = customerList[0].CustomerId, FollowUpDate = DateTime.UtcNow.AddDays(-10), Subject = "Kickoff requirements discussion", FollowUpType = FollowUpType.Meeting, Status = FollowUpStatus.Completed, CompletedDate = DateTime.UtcNow.AddDays(-10), AssignedTo = sales1.Id, Remarks = "Client requirements documented." },
                new() { CustomerId = customerList[1].CustomerId, FollowUpDate = DateTime.UtcNow.AddDays(-8), Subject = "Deliver SOW and rate card", FollowUpType = FollowUpType.Email, Status = FollowUpStatus.Completed, CompletedDate = DateTime.UtcNow.AddDays(-8), AssignedTo = sales1.Id, Remarks = "SOW approved by TCS team." },
                new() { CustomerId = customerList[2].CustomerId, FollowUpDate = DateTime.UtcNow.AddDays(-5), Subject = "Review cloud security specs", FollowUpType = FollowUpType.Call, Status = FollowUpStatus.Completed, CompletedDate = DateTime.UtcNow.AddDays(-5), AssignedTo = sales2.Id, Remarks = "Security checklist cleared." },
                new() { OpportunityId = oppList[3].OpportunityId, FollowUpDate = DateTime.UtcNow.AddDays(-3), Subject = "Budget negotiation meeting", FollowUpType = FollowUpType.Meeting, Status = FollowUpStatus.Completed, CompletedDate = DateTime.UtcNow.AddDays(-3), AssignedTo = sales2.Id, Remarks = "Pricing proposal updated to final discount tier." },

                // Today
                new() { CustomerId = customerList[3].CustomerId, OpportunityId = oppList[3].OpportunityId, FollowUpDate = DateTime.UtcNow.Date, Subject = "Confirm board approval for contract", FollowUpType = FollowUpType.Call, Status = FollowUpStatus.Planned, AssignedTo = sales2.Id, Remarks = "Call scheduled for 3 PM IST." },
                new() { CustomerId = customerList[4].CustomerId, OpportunityId = oppList[4].OpportunityId, FollowUpDate = DateTime.UtcNow.Date, Subject = "Share revised technical proposal", FollowUpType = FollowUpType.Email, Status = FollowUpStatus.Planned, AssignedTo = sales1.Id, Remarks = "Send updated proposal PDF." },
                new() { LeadId = 1, FollowUpDate = DateTime.UtcNow.Date, Subject = "Follow up on demo feedback", FollowUpType = FollowUpType.Call, Status = FollowUpStatus.Planned, AssignedTo = sales1.Id, Remarks = "Check if CEO watched demo." },

                // Upcoming
                new() { OpportunityId = oppList[5].OpportunityId, FollowUpDate = DateTime.UtcNow.AddDays(2), Subject = "Technical architecture presentation", FollowUpType = FollowUpType.Meeting, Status = FollowUpStatus.Planned, AssignedTo = sales2.Id, Remarks = "Prepare architecture diagrams." },
                new() { OpportunityId = oppList[6].OpportunityId, FollowUpDate = DateTime.UtcNow.AddDays(3), Subject = "Commercial terms finalization", FollowUpType = FollowUpType.Meeting, Status = FollowUpStatus.Planned, AssignedTo = sales1.Id, Remarks = "Legal contract review." },
                new() { CustomerId = customerList[7].CustomerId, FollowUpDate = DateTime.UtcNow.AddDays(5), Subject = "Fleet IoT device testing check-in", FollowUpType = FollowUpType.Task, Status = FollowUpStatus.Planned, AssignedTo = sales2.Id, Remarks = "Review test report with engineers." },
                new() { CustomerId = customerList[8].CustomerId, FollowUpDate = DateTime.UtcNow.AddDays(7), Subject = "Send case study and customer references", FollowUpType = FollowUpType.Email, Status = FollowUpStatus.Planned, AssignedTo = sales1.Id, Remarks = "Send ICICI references from BFSI sector." },
                new() { OpportunityId = oppList[11].OpportunityId, FollowUpDate = DateTime.UtcNow.AddDays(9), Subject = "Security audit walkthrough", FollowUpType = FollowUpType.Meeting, Status = FollowUpStatus.Planned, AssignedTo = sales2.Id, Remarks = "Coordinate with CISO office." },
                new() { LeadId = 3, FollowUpDate = DateTime.UtcNow.AddDays(4), Subject = "Discovery call with VP of Engineering", FollowUpType = FollowUpType.Call, Status = FollowUpStatus.Planned, AssignedTo = sales1.Id, Remarks = "Discuss API integration volume." },

                // Missed (past date without completion)
                new() { CustomerId = customerList[14].CustomerId, FollowUpDate = DateTime.UtcNow.AddDays(-4), Subject = "Annual maintenance renewal notice", FollowUpType = FollowUpType.Email, Status = FollowUpStatus.Missed, AssignedTo = sales1.Id, Remarks = "Client unresponsive." },
                new() { LeadId = 4, FollowUpDate = DateTime.UtcNow.AddDays(-2), Subject = "Follow up on pricing sheet", FollowUpType = FollowUpType.Call, Status = FollowUpStatus.Missed, AssignedTo = sales1.Id, Remarks = "Call was not answered." }
            };

            context.FollowUps.AddRange(followUps);
            await context.SaveChangesAsync();
        }

        // 7. Seed Activities (10 activities)
        if (!await context.Activities.AnyAsync())
        {
            var activities = new List<Activity>
            {
                new() { ActivityType = ActivityType.Call, Subject = "Introductory call with Reliance VP", Description = "Discussed digital transformation requirements and timeline.", ActivityDate = DateTime.UtcNow.AddDays(-25), CustomerId = customerList[0].CustomerId, AssignedTo = sales1.Id, Status = ActivityStatus.Completed },
                new() { ActivityType = ActivityType.Meeting, Subject = "Product demonstration to TCS leadership", Description = "Demonstrated core CRM & analytics dashboard capabilities.", ActivityDate = DateTime.UtcNow.AddDays(-20), CustomerId = customerList[1].CustomerId, AssignedTo = sales1.Id, Status = ActivityStatus.Completed },
                new() { ActivityType = ActivityType.Email, Subject = "Sent commercial proposal for Infosys", Description = "Emailed tiered pricing schedule for 500 users.", ActivityDate = DateTime.UtcNow.AddDays(-15), CustomerId = customerList[2].CustomerId, AssignedTo = sales2.Id, Status = ActivityStatus.Completed },
                new() { ActivityType = ActivityType.Meeting, Subject = "HDFC Bank Security and Compliance review", Description = "Answered ISO 27001 and RBI data locality compliance questions.", ActivityDate = DateTime.UtcNow.AddDays(-10), CustomerId = customerList[3].CustomerId, AssignedTo = sales2.Id, Status = ActivityStatus.Completed },
                new() { ActivityType = ActivityType.Call, Subject = "Quarterly check-in with Wipro Account Head", Description = "Received positive feedback on current deployment sprint.", ActivityDate = DateTime.UtcNow.AddDays(-5), CustomerId = customerList[4].CustomerId, AssignedTo = sales1.Id, Status = ActivityStatus.Completed },
                new() { ActivityType = ActivityType.Task, Subject = "Drafted technical SLA document for Airtel", Description = "Completed draft 99.99% availability SLA.", ActivityDate = DateTime.UtcNow.AddDays(-3), CustomerId = customerList[5].CustomerId, AssignedTo = sales2.Id, Status = ActivityStatus.Completed },
                new() { ActivityType = ActivityType.Meeting, Subject = "On-site visit to Adani Logistics port office", Description = "Assessed field conditions for barcode scanner integration.", ActivityDate = DateTime.UtcNow.AddDays(-2), CustomerId = customerList[6].CustomerId, AssignedTo = sales1.Id, Status = ActivityStatus.Completed },
                new() { ActivityType = ActivityType.Call, Subject = "Cold outreach to Rajesh Gupta (Delhi Corp)", Description = "Introduced our CRM services; prospect requested deck.", ActivityDate = DateTime.UtcNow.AddDays(-1), LeadId = 1, AssignedTo = sales1.Id, Status = ActivityStatus.Completed },
                new() { ActivityType = ActivityType.Email, Subject = "Sent product brochure to Sneha Kulkarni", Description = "Shared marketing collateral and case studies.", ActivityDate = DateTime.UtcNow.AddHours(-12), LeadId = 2, AssignedTo = sales1.Id, Status = ActivityStatus.Completed },
                new() { ActivityType = ActivityType.Task, Subject = "Setup sandbox demo environment for Arjun Nambiar", Description = "Configured custom fields and test leads in trial tenant.", ActivityDate = DateTime.UtcNow.AddHours(-4), LeadId = 3, AssignedTo = sales1.Id, Status = ActivityStatus.Completed }
            };

            context.Activities.AddRange(activities);
            await context.SaveChangesAsync();
        }

        // 8. Seed Initial Audit Log
        if (!await context.AuditLogs.AnyAsync())
        {
            context.AuditLogs.Add(new AuditLog
            {
                UserId = admin.Id,
                UserName = admin.Email,
                Action = "SystemSeed",
                Module = "System",
                RecordId = "INIT",
                OldValue = null,
                NewValue = "Initial database seed completed.",
                Result = "Success",
                Details = "Seeded users, customers, leads, opportunities, follow-ups, and activities.",
                CreatedDate = DateTime.UtcNow,
                IpAddress = "127.0.0.1"
            });
            await context.SaveChangesAsync();
        }
    }
}

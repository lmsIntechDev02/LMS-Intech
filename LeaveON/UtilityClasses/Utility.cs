using LMS.Constants;
using Repository.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Web;

namespace LeaveON.UtilityClasses
{
  public static class Utility
  {
    public static LeaveONEntities db = new LeaveONEntities();

    private static List<AspNetUser> aspNetUserNames;
    public static List<AspNetUser> AspNetUserNames
    {
      get
      {
        if (aspNetUserNames == null)
        {
          // Retrieve users without modifying the database
          var usersFromDb = db.AspNetUsers.ToList();

          // Format usernames for display in UI without changing database values
          aspNetUserNames = usersFromDb.Select(user =>
          {
            var emailParts = user.UserName.Split('@')[0].Split('.');
            var formattedName = string.Join(" ", emailParts.Select(part => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(part)));

            return new AspNetUser
            {
              Id = user.Id,
              UserName = formattedName,
              // Include other necessary properties
            };
          }).ToList();
        }
        return aspNetUserNames;
      }
      set { aspNetUserNames = value; }
    }
    public static int GetCasualLeaveCountFromShortLeave(string UserId)
    {
      List<Leave> shortLeaves = db.Leaves.Where(x => x.UserId == UserId && x.IsShortLeave == true && x.IsAccepted1>0 && x.IsAccepted2> 0).ToList();
      //shortLeaves.Select(x=>x.)
      double ShortLeaveHours = 0;
      foreach (Leave sleave in shortLeaves)
      {
        ShortLeaveHours += (sleave.EndDate - sleave.StartDate).TotalHours;
      }
      return (int)(ShortLeaveHours / 8);
    }
    public static int GetAdditionalCasualLeaveCountFromShortLeave(string UserId, int existingTaken)
    {
      List<Leave> shortLeaves = db.Leaves
          .Where(x => x.UserId == UserId && x.IsShortLeave == true && x.IsAccepted1 > 0 && x.IsAccepted2 > 0)
          .ToList();

      double ShortLeaveHours = 0;
      foreach (Leave sleave in shortLeaves)
      {
        ShortLeaveHours += (sleave.EndDate - sleave.StartDate).TotalHours;
      }

      int totalDaysTaken = (int)(ShortLeaveHours / 8);

      int additionalDaysTaken = totalDaysTaken - existingTaken;

      return additionalDaysTaken > 0 ? additionalDaysTaken : 0;
    }
    public static List<LeaveType> LeaveTypesBasedOnGender(string UserId)
    {
      using (var db = new LeaveONEntities()) // Replace with your context
      {
        AspNetUser aspNetUser = db.AspNetUsers.FirstOrDefault(x => x.Id == UserId);

        List<LeaveType> leaveTypes;
        LeaveType leaveType;
        if (aspNetUser != null && aspNetUser.Gender == false)
        {
          leaveTypes = db.LeaveTypes.ToList();
        }
        else
        {
          leaveType = db.LeaveTypes.FirstOrDefault(x => x.Id == Consts.MaternityLeaveTypeId);
          leaveTypes = db.LeaveTypes.ToList();
          leaveTypes.Remove(leaveType);
        }

        return leaveTypes;
      }
    }
    public static List<LeaveType> FilteredLeavesTaken (string UserId, int policyId)
    {
      var leaveTypes = db.LeaveTypes.ToList();
      var filteredLeaves = LeaveTypesBasedOnGender(UserId);
      
      var policyDetailLeaves = db.UserLeavePolicyDetails.ToList();
      var excludedIds = new HashSet<int> { 7, 8, 9, 10 }; // Assuming IDs are integers

      var leaveBalance = db.LeaveBalances
          .Where(leave =>
              leave.UserId == UserId &&
              leave.UserLeavePolicyId == policyId &&
              leave.Balance < 0)
          .Select(policyDetail => policyDetail.LeaveTypeId)
          .ToList();

      leaveBalance.RemoveAll(id => excludedIds.Contains((int)id));




      var policyDetailLeaveTypeIds = db.UserLeavePolicyDetails
      .Where(policyDetail => policyDetail.UserLeavePolicyId == policyId)
      .Select(policyDetail => policyDetail.LeaveTypeId)
      .ToList();

      filteredLeaves = filteredLeaves
      .Where(leave => policyDetailLeaveTypeIds.Contains(leave.Id))
      .ToList();


      filteredLeaves = filteredLeaves
      .Where(leave => !leaveBalance.Contains(leave.Id))
      .ToList();

      /*  var findCompensatoryLeave = db.LeaveBalances.FirstOrDefault(x => x.LeaveTypeId == 0 && x.UserId == UserId && x.UserLeavePolicyId == policyId && x.Balance > 0);
         if (findCompensatoryLeave != null)
         {
           // Add compensatory leave to filteredLeaves if found
           var compensatoryLeaveType = leaveTypes.FirstOrDefault(leaveType => leaveType.Id == findCompensatoryLeave.LeaveTypeId);
           if (compensatoryLeaveType != null)
           {
             filteredLeaves.Add(compensatoryLeaveType);
           }
         }*/
     
      var compensatoryLeaveType = leaveTypes.FirstOrDefault(leaveType => leaveType.Id == 0);
      if (compensatoryLeaveType != null)
      {
        filteredLeaves.Add(compensatoryLeaveType);
      }


      return filteredLeaves;
    }
    public static async Task AdjustLeaveBalance(decimal userLeavePolicyId)
    {
      // Fetch updated policy details
        var updatedPolicyDetails = await db.UserLeavePolicyDetails
          .Where(detail => detail.UserLeavePolicyId == userLeavePolicyId)
          .ToListAsync();

      // Fetch LeaveBalance records associated with the specified UserLeavePolicyId
      var leaveBalancesToUpdate = await db.LeaveBalances
          .Where(balance => balance.UserLeavePolicyId == userLeavePolicyId)
          .ToListAsync();

      // Update the leave balances based on updated policy details
      foreach (var balance in leaveBalancesToUpdate)
      {
        // Find the corresponding policy detail for the leave type
        var correspondingDetail = updatedPolicyDetails.FirstOrDefault(detail => detail.LeaveTypeId == balance.LeaveTypeId);

        if (correspondingDetail != null)
        {
          // Update the balance based on the corresponding policy detail
          balance.Balance = correspondingDetail.Allowed - balance.Taken;
          // You might need additional logic here based on your business rules
          // For instance, handling weekends or country-specific rules
        }
      }

      // Save changes to the database
      await db.SaveChangesAsync();
    }





    //public static List<AspNetUser> ConvertEmailsToNames()
    //{
    //  AspNetUserNames = db.AspNetUsers.ToList();
    //  foreach (AspNetUser aspNetUser in AspNetUserNames)
    //  {
    //    aspNetUser.UserName = aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");
    //  }
    //  return AspNetUserNames;
    //}


  }
}

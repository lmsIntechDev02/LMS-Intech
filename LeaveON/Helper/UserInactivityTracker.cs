using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Runtime.Caching;

namespace LeaveON.Helper
{

  

  public static class UserInactivityTracker
  {
    private static readonly ObjectCache Cache =
        MemoryCache.Default;

    public static bool IsInactive(
        string sessionKey,
        TimeSpan timeout)
    {
      var lastActivity = Cache.Get(sessionKey) as DateTime?;

      if (!lastActivity.HasValue)
        return true;

      return DateTime.UtcNow - lastActivity.Value > timeout;
    }

    public static void UpdateActivity(string sessionKey)
    {
      Cache.Set(
          sessionKey,
          DateTime.UtcNow,
          DateTimeOffset.UtcNow.AddDays(2));
    }

    public static void Remove(string sessionKey)
    {
      Cache.Remove(sessionKey);
    }
  }
}

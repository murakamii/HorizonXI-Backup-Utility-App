using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Toolkit.Uwp.Notifications;

namespace HorizonXIBackupApp.Services;

public sealed class NotificationService
{
    /// <summary>
    /// Show a Windows toast notification summarising addon updates.
    /// Silent no-op if updates is null or empty, or if Windows toast is unavailable.
    /// </summary>
    public void ShowUpdateSummary(IReadOnlyList<string> updatedAddons)
    {
        if (updatedAddons is null || updatedAddons.Count == 0) return;

        try
        {
            var preview = updatedAddons.Take(4).ToList();
            var body = string.Join(", ", preview);
            if (updatedAddons.Count > preview.Count)
            {
                body += $", +{updatedAddons.Count - preview.Count} more";
            }

            var title = updatedAddons.Count == 1
                ? "HorizonXI: 1 addon update available"
                : $"HorizonXI: {updatedAddons.Count} addon updates available";

            new ToastContentBuilder()
                .AddText(title)
                .AddText(body)
                .Show();
        }
        catch
        {
            // Toast unavailable - silently ignore. Status is also shown in the UI.
        }
    }

    public void ShowTest()
    {
        try
        {
            new ToastContentBuilder()
                .AddText("HorizonXI Backup Utility")
                .AddText("If you can see this, toast notifications are working.")
                .Show();
        }
        catch
        {
        }
    }
}

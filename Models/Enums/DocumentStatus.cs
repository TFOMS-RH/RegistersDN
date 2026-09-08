using System;

namespace RegistrDN.Models.Enums
{
    public enum DocumentStatus
    {
        Uploaded = 0,
        Checking = 1,
        Ready = 2,
        Error = 3
    }

    public static class DocumentStatusExtensions
    {
        public static string GetDisplayName(this DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Uploaded => "📥 Загружен",
                DocumentStatus.Checking => "⏳ Проверяется",
                DocumentStatus.Ready => "✅ Готов",
                DocumentStatus.Error => "❌ Ошибка",
                _ => "—"
            };
        }

        public static string GetCssClass(this DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Uploaded => "info",
                DocumentStatus.Checking => "warning",
                DocumentStatus.Ready => "success",
                DocumentStatus.Error => "error",
                _ => "secondary"
            };
        }
    }
}
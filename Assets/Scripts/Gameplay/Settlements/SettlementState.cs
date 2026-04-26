using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    [Serializable]
    public class SettlementState
    {
        public string SettlementId;
        public string SettlementName;
        public int LastAdvancedTurn;

        private readonly Dictionary<string, RepairableSiteState> _sites = new Dictionary<string, RepairableSiteState>();
        private readonly HashSet<string> _conditions = new HashSet<string>();
        private readonly List<string> _pendingMessages = new List<string>();

        public IReadOnlyDictionary<string, RepairableSiteState> Sites => _sites;

        public RepairableSiteState GetSite(string siteId)
        {
            RepairableSiteState site;
            return siteId != null && _sites.TryGetValue(siteId, out site) ? site : null;
        }

        public void SetSite(RepairableSiteState site)
        {
            if (site == null || string.IsNullOrEmpty(site.SiteId))
                return;

            _sites[site.SiteId] = site;
        }

        public bool HasCondition(string condition)
        {
            return !string.IsNullOrEmpty(condition) && _conditions.Contains(condition);
        }

        public void SetCondition(string condition, bool enabled)
        {
            if (string.IsNullOrEmpty(condition))
                return;

            if (enabled)
                _conditions.Add(condition);
            else
                _conditions.Remove(condition);
        }

        public void AddPendingMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                _pendingMessages.Add(message);
        }

        public List<string> ConsumePendingMessages()
        {
            var result = new List<string>(_pendingMessages);
            _pendingMessages.Clear();
            return result;
        }

        public List<string> GetConditionsSnapshot()
        {
            return new List<string>(_conditions);
        }

        public List<string> GetPendingMessagesSnapshot()
        {
            return new List<string>(_pendingMessages);
        }

        public void RestoreCollections(
            Dictionary<string, RepairableSiteState> sites,
            IEnumerable<string> conditions,
            IEnumerable<string> pendingMessages)
        {
            _sites.Clear();
            if (sites != null)
            {
                foreach (var kvp in sites)
                {
                    if (kvp.Value != null)
                        _sites[kvp.Key] = kvp.Value;
                }
            }

            _conditions.Clear();
            if (conditions != null)
            {
                foreach (string condition in conditions)
                {
                    if (!string.IsNullOrEmpty(condition))
                        _conditions.Add(condition);
                }
            }

            _pendingMessages.Clear();
            if (pendingMessages != null)
            {
                foreach (string message in pendingMessages)
                {
                    if (!string.IsNullOrWhiteSpace(message))
                        _pendingMessages.Add(message);
                }
            }
        }
    }
}

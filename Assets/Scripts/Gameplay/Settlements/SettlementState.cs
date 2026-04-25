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
        private readonly Dictionary<string, HouseDramaState> _dramas = new Dictionary<string, HouseDramaState>();
        private readonly HashSet<string> _conditions = new HashSet<string>();
        private readonly List<string> _pendingMessages = new List<string>();

        public IReadOnlyDictionary<string, RepairableSiteState> Sites => _sites;
        public IReadOnlyDictionary<string, HouseDramaState> Dramas => _dramas;

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

        public HouseDramaState GetDrama(string dramaId)
        {
            HouseDramaState drama;
            return dramaId != null && _dramas.TryGetValue(dramaId, out drama) ? drama : null;
        }

        public void SetDrama(HouseDramaState drama)
        {
            if (drama == null || string.IsNullOrEmpty(drama.DramaId))
                return;

            _dramas[drama.DramaId] = drama;
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
    }
}

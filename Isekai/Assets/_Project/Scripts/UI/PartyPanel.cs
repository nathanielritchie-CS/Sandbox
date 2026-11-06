using System.Text;
using Isekai.Core.Events;
using UnityEngine;

namespace Isekai.UI
{
    public class PartyPanel : MonoBehaviour
    {
        [SerializeField] private PartyEventChannel _party;
        [SerializeField] private UnityEngine.UI.Text _text;

        private void OnEnable()
        {
            _party.Raised += Show;
            Show(_party.Current);
        }

        private void OnDisable() => _party.Raised -= Show;

        private void Show(PartyStatus status)
        {
            var sb = new StringBuilder();
            if (status != null && status.Members.Length > 0)
            {
                sb.Append("Party\n");
                for (var i = 0; i < status.Members.Length; i++)
                {
                    sb.Append(status.Members[i]);
                    if (i == 0) sb.Append(" (leader)");
                    if (i == status.Self) sb.Append(" (you)");
                    sb.Append('\n');
                }
                sb.Append('\n');
            }
            if (status != null && status.InviteFrom != null)
                sb.Append(status.InviteFrom).Append(" invites you to a party.\nPress E to accept.\n\n");
            sb.Append("E: invite nearby / accept\nL: leave party");
            _text.text = sb.ToString();
        }
    }
}

using Isekai.Gameplay.Appearance;
using Unity.Collections;
using Unity.Netcode;

namespace Isekai.Net
{
    // A player's look: the owner sends its saved Otherworlder once; the server decodes it against the catalog (bad or
    // missing data becomes defaults) and shares it; every peer shows it, late joiners included.
    public class PlayerAppearance : NetworkBehaviour
    {
        [UnityEngine.SerializeField] private AppearanceApplier _applier;

        private readonly NetworkVariable<FixedString128Bytes> _look = new(); // Base64 of the encoded look

        public override void OnNetworkSpawn()
        {
            _look.OnValueChanged += OnLookChanged;
            Show(_look.Value);
            if (!IsOwner) return;
            var catalog = _applier.Catalog;
            var save = CharacterSave.Load();
            var colors = save != null ? save.ColorsFor(catalog.Ids(), catalog.Defaults()) : catalog.Defaults();
            SetLookRpc(AppearanceCodec.Encode(colors));
        }

        public override void OnNetworkDespawn() => _look.OnValueChanged -= OnLookChanged;

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetLookRpc(byte[] bytes)
        {
            var colors = AppearanceCodec.Decode(bytes, _applier.Catalog.Defaults());
            _look.Value = new FixedString128Bytes(AppearanceCodec.ToText(AppearanceCodec.Encode(colors)));
        }

        private void OnLookChanged(FixedString128Bytes previous, FixedString128Bytes current) => Show(current);

        private void Show(FixedString128Bytes look)
        {
            if (look.Length == 0) return; // not sent yet: the materials' own colours are the defaults
            _applier.Apply(AppearanceCodec.Decode(AppearanceCodec.FromText(look.ToString()), _applier.Catalog.Defaults()));
        }
    }
}

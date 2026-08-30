namespace Northwild
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact(PlayerInventory inventory);
    }
}

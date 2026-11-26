namespace Flame_n_Blame_Challenge.Domain
{
    public interface IUsable
    {
        public float UseCoolDown { get; }
        public void Use();
    }
}

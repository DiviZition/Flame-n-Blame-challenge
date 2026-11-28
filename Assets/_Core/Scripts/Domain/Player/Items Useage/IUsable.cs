namespace Flame_n_Blame_Challenge.Domain
{
    public interface IUsable
    {
        public float UseCoolDown { get; }
        //TODO: Нет времени на реализацию возврата UseContext/UseEffectsOutput.
        //С одним предметом, пока делаем костыль, но на будущее, нельзя давать предметам ссылку на домен
        public void Use(Player playerdomain); 
    }
}

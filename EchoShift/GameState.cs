// using не требуется, так как класс не использует MonoGame напрямую
public class GameState
{
    public int Kills { get; set; }
    public int EchoCharge { get; set; }
    public int EchoCharges { get; set; }
    public float PlayerSpeedBonus { get; set; }
    public int PlayerDamageBonus { get; set; }
    public float ShootSpeedBonus { get; set; }
    public int BulletSizeBonus { get; set; }

    public GameState()
    {
        Kills = 0;
        EchoCharge = 0;
        EchoCharges = 0;
        PlayerSpeedBonus = 1f;
        PlayerDamageBonus = 0;
        ShootSpeedBonus = 1f;
        BulletSizeBonus = 0;
    }
}
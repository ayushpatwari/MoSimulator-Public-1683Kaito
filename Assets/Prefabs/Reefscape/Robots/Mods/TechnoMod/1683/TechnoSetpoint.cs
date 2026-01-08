using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods._1683Mod._1683
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Techno Setpoint", order = 0)]
    public class TechnoSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Deg")] public float elevatorAngle;
        [Tooltip("Deg")] public float intakeAngle;
    }
}
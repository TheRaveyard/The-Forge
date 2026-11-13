using UnityEngine;

public enum OreType
{
    Gold, 
    Bronze, 
    Copper,

}
public class MaterialDefinition : MonoBehaviour
{
    public OreType oreType;
    public OreType Type => oreType;

}

using UnityEngine;

public enum OreType
{
    Gold, 
    Bronze, 
    Copper,

}
public class MaterialDefinition : MonoBehaviour
{
    private OreType oreType;
    public OreType Type => oreType;

}

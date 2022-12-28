
struct Attributes
{
};
    
struct Varyings
{
};
    
Varyings HiddenVertex(Attributes input)
{
    return (Varyings)0;
}

half4 HiddenFragment(Varyings input) : SV_Target
{
    return 0;
}
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;

namespace Una.Drawing;

public class SeStringDirectiveParser : IUdtDirectiveParser
{
    public string Name { get; } = "sestringvalue";
    
    public void Parse(Node node, string value)
    {
        
        SeStringBuilder builder = new();

        builder.AppendMacroString(value);
        
        node.NodeValue = builder.Build();
    }
}

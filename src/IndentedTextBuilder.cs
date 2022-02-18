using System.CodeDom.Compiler;

namespace AsyncFluentGenerator;

internal class IndentedTextBuilder : IndentedTextWriter
{
    public IndentedTextBuilder(TextWriter writer) : base(writer) {}
    
    public override string ToString()
    {
        return InnerWriter.ToString();
    }

    public override void Close()
    {
        InnerWriter.Close();
        base.Close();
    }

    public new IndentedTextBuilder Write(string s)
    {
        base.Write(s);
        return this;
    }

    public new IndentedTextBuilder WriteLine(string s)
    {
        base.WriteLine(s);
        return this;
    }

    public IndentedTextBuilder WriteBeginScope(bool withParenthese = true)
    {
        if (withParenthese)
        {
            base.WriteLine("{");
        }
        Indent++;
        return this;
    }

    public IndentedTextBuilder WriteEndScope(bool withParenthese = true)
    {
        Indent--;
        if (withParenthese)
        {
            base.WriteLine("}");
        }
        return this;
    }
}
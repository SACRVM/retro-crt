using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class WithSinkIntegrationTests
{
    [Fact]
    public void WithSink_routes_crt_writes_to_the_override()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        using var sink = new StringWriter();

        using (Crt.WithSink(sink))
        {
            Crt.WriteLine("hello");
            Crt.TextColor(Color.LightCyan);
        }

        Assert.Contains("hello", sink.ToString());
        Assert.Contains("\x1b[", sink.ToString());
        // ConsoleCapture's stdout buffer must be untouched.
        Assert.DoesNotContain("hello", c.Out);
    }

    [Fact]
    public void WithSink_routes_banner_progressbar_typewriter_to_one_writer()
    {
        using var c = ConsoleCapture.Start(ansi: true, unicode: true);
        using var sink = new StringWriter();

        using (Crt.WithSink(sink))
        {
            Banner.Box("hi");
            using (var bar = ProgressBar.Start(total: 100, width: 5))
                bar.Set(50);
            Typewriter.Type("x", msPerChar: 0);
        }

        var captured = sink.ToString();
        Assert.Contains("hi", captured);
        Assert.Contains("░", captured); // empty bar glyph
        Assert.Contains("x", captured);

        // Console.Out (which ConsoleCapture redirects) must stay clean.
        Assert.DoesNotContain("hi", c.Out);
        Assert.DoesNotContain("░", c.Out);
    }

    [Fact]
    public void WithSink_also_routes_log_streams()
    {
        using var c = ConsoleCapture.Start(ansi: false);
        using var sink = new StringWriter();

        using (Crt.WithSink(sink))
        {
            Log.Info("hello");
            Log.Error("boom");
        }

        Assert.Contains("INFO", sink.ToString());
        Assert.Contains("ERROR", sink.ToString());
        Assert.DoesNotContain("INFO", c.Out);
        Assert.DoesNotContain("ERROR", c.Err);
    }

    [Fact]
    public void WithSink_dispose_restores_console_routing()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        using var sink = new StringWriter();

        using (Crt.WithSink(sink))
            Crt.WriteLine("inside");

        Crt.WriteLine("outside");

        Assert.Contains("inside", sink.ToString());
        Assert.DoesNotContain("inside", c.Out);
        Assert.Contains("outside", c.Out);
    }

    [Fact]
    public void WithSink_nests_and_restores_inner_to_outer()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        using var outer = new StringWriter();
        using var inner = new StringWriter();

        using (Crt.WithSink(outer))
        {
            Crt.WriteLine("o1");
            using (Crt.WithSink(inner))
                Crt.WriteLine("inner");
            Crt.WriteLine("o2");
        }

        Assert.Contains("o1",    outer.ToString());
        Assert.Contains("o2",    outer.ToString());
        Assert.Contains("inner", inner.ToString());
        Assert.DoesNotContain("inner", outer.ToString());
    }

    [Fact]
    public void Sink_property_returns_console_out_by_default()
    {
        using var c = ConsoleCapture.Start(ansi: true);

        // No WithSink scope active; Crt.Sink must follow Console.SetOut,
        // which ConsoleCapture sets to its own StringWriter.
        Crt.Sink.Write("direct");

        Assert.Contains("direct", c.Out);
    }
}

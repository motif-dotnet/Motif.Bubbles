namespace Motif.Bubbles.Specs;

/// <summary>
/// Motif.Tiny: a small made-up Motif repo with one of everything the scanner
/// maps, and a few files it must leave out. Its counts are worked out by hand
/// in the comments, so a spec can claim them.
/// </summary>
public static class TinyRepo
{
    // 4 lines of code: the usings and comments don't count, the braces do.
    public static readonly CodeFile Program = new(
        "Motif.Tiny.App/Program.cs",
        """
        // The composition root.
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddSingleton<ForStoringOrders, ForStoringOrdersViaIndexedDB>();
        builder.Services.AddSingleton<ForSendingMail, ForSendingMailViaSmtp>();

        await builder.Build().RunAsync();
        """);

    // 3 lines of code: the Razor and HTML comments don't count.
    public static readonly CodeFile Shell = new(
        "Motif.Tiny.App/Shell.razor",
        """
        @* The one page. *@
        @inject ForDoing Tasks
        @*
            Orders, newest first.
        *@
        <h1>Orders</h1>
        <!-- One button
             for now. -->
        <button @onclick="Tasks.Place">Place</button>
        """);

    public static readonly CodeFile[] App =
    [
        new("Motif.Tiny.App/Motif.Tiny.App.csproj", "<Project />"),
        Program,
        Shell,
        // The web kit's copy is the suite's code, not the app's.
        new("Motif.Tiny.App/Suite/Toaster.cs",
            "namespace Motif.Suite;\npublic class Toaster { }\n"),
        new("Motif.Tiny.App/wwwroot/css/motif.css", "body { margin: 0; }\n"),
        new("Motif.Tiny.App/wwwroot/css/app.css", ".orders { gap: 8px; }\n"),
        // Build output.
        new("Motif.Tiny.App/bin/Debug/Motif.Tiny.App.cs", "class Built { }\n"),
    ];

    // 7 lines of code, 2 types.
    public static readonly CodeFile Order = new(
        "Motif.Tiny/Motif.Tiny.Domain/Order.cs",
        """
        namespace Motif.Tiny;

        /// <summary>
        /// An order.
        /// </summary>
        public sealed record Order(int Id, string Item);

        public enum OrderState
        {
            Placed,
            Sent,
        }
        """);

    public static readonly CodeFile[] Domain =
    [
        new("Motif.Tiny/Motif.Tiny.Domain/Motif.Tiny.Domain.csproj",
            "<Project />"),
        Order,
    ];

    // 2 tasks.
    public static readonly CodeFile ForDoing = new(
        "Motif.Tiny/Motif.Tiny.UseCases/ForDoing.cs",
        """
        namespace Motif.Tiny;

        public interface ForDoing
        {
            /// <summary>
            /// Places an order.
            /// </summary>
            Task Place(
                string item);

            void Cancel(int id);
        }
        """);

    // 1 query.
    public static readonly CodeFile ForSeeing = new(
        "Motif.Tiny/Motif.Tiny.UseCases/ForSeeing.cs",
        """
        namespace Motif.Tiny;

        public interface ForSeeing
        {
            IReadOnlyList<Order> Orders { get; }
        }
        """);

    public static readonly CodeFile Application = new(
        "Motif.Tiny/Motif.Tiny.UseCases/MotifTinyApplication.cs",
        """
        namespace Motif.Tiny;

        public sealed class MotifTinyApplication(
            ForStoringOrders orders, ForSendingMail mail) : ForDoing, ForSeeing
        {
            public IReadOnlyList<Order> Orders { get; private set; } = [];

            public async Task Place(string item)
            {
                await orders.Save(new Order(Orders.Count + 1, item));
                await mail.Send(new Mail("Placed", item));
            }

            public void Cancel(int id) { }
        }
        """);

    public static readonly CodeFile ForStoringOrders = new(
        "Motif.Tiny/Motif.Tiny.UseCases/Ports/ForStoringOrders.cs",
        """
        namespace Motif.Tiny;

        public interface ForStoringOrders
        {
            Task Save(Order order);

            Task<IReadOnlyList<Order>> All();
        }
        """);

    public static readonly CodeFile ForSendingMail = new(
        "Motif.Tiny/Motif.Tiny.UseCases/Ports/ForSendingMail.cs",
        """
        namespace Motif.Tiny;

        public interface ForSendingMail
        {
            Task Send(Mail mail);
        }
        """);

    // Not a port: a type the port takes, so it's part of the Application.
    public static readonly CodeFile Mail = new(
        "Motif.Tiny/Motif.Tiny.UseCases/Ports/Mail.cs",
        """
        namespace Motif.Tiny;

        public sealed record Mail(string Subject, string Body);
        """);

    public static readonly CodeFile[] UseCases =
    [
        new("Motif.Tiny/Motif.Tiny.UseCases/Motif.Tiny.UseCases.csproj",
            "<Project />"),
        ForDoing,
        ForSeeing,
        Application,
        ForStoringOrders,
        ForSendingMail,
        Mail,
    ];

    public static readonly CodeFile IndexedDB = new(
        "Adapters/Motif.Tiny.ForStoringOrders/ForStoringOrdersViaIndexedDB.cs",
        """
        namespace Motif.Tiny.Adapters;

        public sealed class ForStoringOrdersViaIndexedDB(Shelf shelf)
            : ForStoringOrders
        {
            public Task Save(Order order) => shelf.Put($"{order.Id}", order.Item);

            public Task<IReadOnlyList<Order>> All() => throw new NotSupportedException();
        }
        """);

    // The adapter's Seam and its real Fitting: part of the adapter.
    public static readonly CodeFile[] StoringOrders =
    [
        new("Adapters/Motif.Tiny.ForStoringOrders/Motif.Tiny.ForStoringOrders.csproj",
            "<Project />"),
        IndexedDB,
        new("Adapters/Motif.Tiny.ForStoringOrders/Shelf.cs",
            """
            namespace Motif.Tiny.Adapters;

            public interface Shelf
            {
                Task Put(string key, string value);
            }
            """),
        new("Adapters/Motif.Tiny.ForStoringOrders/JsInteropShelf.cs",
            """
            namespace Motif.Tiny.Adapters;

            public sealed class JsInteropShelf : Shelf
            {
                public Task Put(string key, string value) => Task.CompletedTask;
            }
            """),
        new("Adapters/Motif.Tiny.ForStoringOrders/wwwroot/shelf.js",
            "export function put(key, value) { }\n"),
    ];

    public static readonly CodeFile Smtp = new(
        "Adapters/Motif.Tiny.ForSendingMail/ForSendingMailViaSmtp.cs",
        """
        namespace Motif.Tiny.Adapters;

        public sealed class ForSendingMailViaSmtp : ForSendingMail
        {
            public Task Send(Mail mail) => Task.CompletedTask;
        }
        """);

    public static readonly CodeFile[] SendingMail =
    [
        new("Adapters/Motif.Tiny.ForSendingMail/Motif.Tiny.ForSendingMail.csproj",
            "<Project />"),
        Smtp,
    ];

    public static readonly CodeFile[] Fakes =
    [
        new("Checks/Motif.Tiny.Fakes/Motif.Tiny.Fakes.csproj", "<Project />"),
        new("Checks/Motif.Tiny.Fakes/ForStoringOrders/FakeShelf.cs",
            "namespace Motif.Tiny.Fakes;\npublic sealed class FakeShelf { }\n"),
        new("Checks/Motif.Tiny.Fakes/ForStoringOrders/FullShelf.cs",
            "namespace Motif.Tiny.Fakes;\npublic sealed class FullShelf { }\n"),
        new("Checks/Motif.Tiny.Fakes/ForSendingMail/FakeMailServer.cs",
            "namespace Motif.Tiny.Fakes;\n"
            + "public sealed class FakeMailServer { }\n"),
    ];

    // 2 Facts and a Theory of two cases: 3 specs.
    public static readonly CodeFile Ordering = new(
        "Checks/Motif.Tiny.Specs/Ordering.cs",
        """
        namespace Motif.Tiny.Specs;

        public sealed class Ordering
        {
            [Fact]
            public void places_an_order() { }

            [Fact]
            public void cancels_one() { }

            [Theory]
            [InlineData(1)]
            [InlineData(2)]
            public void numbers_them(int n) { }
        }
        """);

    public static readonly CodeFile[] Specs =
    [
        new("Checks/Motif.Tiny.Specs/Motif.Tiny.Specs.csproj", "<Project />"),
        Ordering,
    ];

    public static readonly CodeFile[] BrowserSpecs =
    [
        new("Checks/Motif.Tiny.BrowserSpecs/Motif.Tiny.BrowserSpecs.csproj",
            "<Project />"),
        new("Checks/Motif.Tiny.BrowserSpecs/TheShell.cs",
            """
            namespace Motif.Tiny.BrowserSpecs;

            public sealed class TheShell
            {
                [Fact]
                public async Task shows_the_orders() { }
            }
            """),
    ];

    /// <summary>
    /// Every file of the repo, with a file in docs/ that belongs to no
    /// project.
    /// </summary>
    public static IReadOnlyList<CodeFile> All { get; } =
    [
        .. App, .. Domain, .. UseCases, .. StoringOrders, .. SendingMail,
        .. Fakes, .. Specs, .. BrowserSpecs,
        new("docs/Example.cs", "class Example { }\n"),
    ];

    /// <summary>
    /// The repo, without these files.
    /// </summary>
    public static IReadOnlyList<CodeFile> Without(params CodeFile[] files) =>
        [.. All.Except(files)];

    /// <summary>
    /// The repo, with these files added.
    /// </summary>
    public static IReadOnlyList<CodeFile> With(params CodeFile[] files) =>
        [.. All, .. files];
}

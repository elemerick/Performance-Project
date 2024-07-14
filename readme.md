# ASP.NET Core 6 Performance

In this project, we try to make use of the various tools ASP.NET Core provide to build a Highly performant app. we start by using `System.Text.Json` to replace the 
well known `NewtownSoft.Json`, later on we implemented the following
1. Memory Caching
1. Distributed Caching with Redis
1. Asynchronous programming with `async` and `await` directives
1. Benchmarking to assess the overall performance of the project

## VS Code Setup

The `C#` extension is required to use this repo.  I have some other settings that you may be curious about
and they are described in my [VS Code gist](https://gist.github.com/dahlsailrunner/1765b807940e29951ea6bdfb36cd85dd).

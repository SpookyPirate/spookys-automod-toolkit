using System.CommandLine;
using SpookysAutomod.Audio.Services;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Cli.Commands;

public static class AudioCommands
{
    private static Option<bool> _jsonOption = null!;
    private static Option<bool> _verboseOption = null!;

    public static Command Create(Option<bool> jsonOption, Option<bool> verboseOption)
    {
        _jsonOption = jsonOption;
        _verboseOption = verboseOption;

        var audioCommand = new Command("audio", "Audio file operations (FUZ, XWM, WAV)");

        audioCommand.AddCommand(CreateInfoCommand());
        audioCommand.AddCommand(CreateExtractFuzCommand());
        audioCommand.AddCommand(CreateCreateFuzCommand());
        audioCommand.AddCommand(CreateWavToXwmCommand());

        return audioCommand;
    }

    private static IModLogger CreateLogger(bool json, bool verbose) =>
        json ? new SilentLogger() : new ConsoleLogger(verbose);

    private static Command CreateInfoCommand()
    {
        var audioArg = new Argument<string>("audio", "Path to the audio file");

        var cmd = new Command("info", "Get information about an audio file")
        {
            audioArg
        };

        cmd.SetHandler((audio, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new AudioService(logger);

            var result = service.GetInfo(audio);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            fileName = result.Value!.FileName,
                            type = result.Value.Type,
                            fileSize = result.Value.FileSize,
                            audioSize = result.Value.AudioSize,
                            hasLipSync = result.Value.HasLipSync,
                            lipSyncSize = result.Value.LipSyncSize,
                            sampleRate = result.Value.SampleRate,
                            channels = result.Value.Channels,
                            bitsPerSample = result.Value.BitsPerSample
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!).ToJson(true));
                }
            }
            else if (result.Success)
            {
                var info = result.Value!;
                Console.WriteLine($"Audio File: {info.FileName}");
                Console.WriteLine($"Type: {info.Type}");
                Console.WriteLine($"Size: {info.FileSize:N0} bytes");

                if (info.HasLipSync)
                    Console.WriteLine($"Lip Sync: Yes ({info.LipSyncSize:N0} bytes)");

                if (info.AudioSize > 0)
                    Console.WriteLine($"Audio Data: {info.AudioSize:N0} bytes");

                if (info.SampleRate > 0)
                    Console.WriteLine($"Sample Rate: {info.SampleRate} Hz");

                if (info.Channels > 0)
                    Console.WriteLine($"Channels: {info.Channels}");

                if (info.BitsPerSample > 0)
                    Console.WriteLine($"Bits Per Sample: {info.BitsPerSample}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, audioArg, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateExtractFuzCommand()
    {
        var fuzArg = new Argument<string>("fuz", "Path to the FUZ file");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory") { IsRequired = true };

        var cmd = new Command("extract-fuz", "Extract FUZ file to XWM and LIP components")
        {
            fuzArg,
            outputOption
        };

        cmd.SetHandler((fuz, output, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new AudioService(logger);

            var result = service.ExtractFuz(fuz, output);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            xwmPath = result.Value!.XwmPath,
                            lipPath = result.Value.LipPath,
                            outputDirectory = result.Value.OutputDirectory
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!).ToJson(true));
                }
            }
            else if (result.Success)
            {
                Console.WriteLine($"Extracted FUZ to: {result.Value!.OutputDirectory}");
                Console.WriteLine($"  XWM: {result.Value.XwmPath}");
                if (result.Value.LipPath != null)
                    Console.WriteLine($"  LIP: {result.Value.LipPath}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, fuzArg, outputOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateCreateFuzCommand()
    {
        var xwmArg = new Argument<string>("xwm", "Path to the XWM audio file");
        var lipOption = new Option<string?>("--lip", "Path to the LIP file (optional)");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output FUZ file path") { IsRequired = true };

        var cmd = new Command("create-fuz", "Create a FUZ file from XWM and LIP")
        {
            xwmArg,
            lipOption,
            outputOption
        };

        cmd.SetHandler((xwm, lip, output, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new AudioService(logger);

            var result = service.CreateFuz(xwm, lip, output);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            outputPath = result.Value
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!).ToJson(true));
                }
            }
            else if (result.Success)
            {
                Console.WriteLine($"Created FUZ: {result.Value}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, xwmArg, lipOption, outputOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateWavToXwmCommand()
    {
        var wavArg = new Argument<string>("wav", "Path to the WAV file");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output XWM file path") { IsRequired = true };

        var cmd = new Command("wav-to-xwm", "Convert WAV to XWM format")
        {
            wavArg,
            outputOption
        };

        cmd.SetHandler((wav, output, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new AudioService(logger);

            var result = service.ConvertWavToXwm(wav, output);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            outputPath = result.Value
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!, suggestions: result.Suggestions).ToJson(true));
                }
            }
            else if (result.Success)
            {
                Console.WriteLine($"Converted to XWM: {result.Value}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                if (result.Suggestions?.Count > 0)
                {
                    Console.Error.WriteLine("\nSuggestions:");
                    foreach (var s in result.Suggestions)
                        Console.Error.WriteLine($"  - {s}");
                }
                Environment.ExitCode = 1;
            }
        }, wavArg, outputOption, _jsonOption, _verboseOption);

        return cmd;
    }
}

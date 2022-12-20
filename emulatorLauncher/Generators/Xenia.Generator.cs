﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using emulatorLauncher.Tools;

namespace emulatorLauncher
{
    class XeniaGenerator : Generator
    {
        private bool _canary = false;

        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            string folderName = (emulator == "xenia-canary" || core == "xenia-canary") ? "xenia-canary" : "xenia";

            string path = AppConfig.GetFullPath(folderName);
            if (string.IsNullOrEmpty(path))
                path = AppConfig.GetFullPath("xenia");

            string exe = Path.Combine(path, "xenia.exe");
            if (!File.Exists(exe))
            {
                _canary = true;
                exe = Path.Combine(path, "xenia-canary.exe");

                if (!File.Exists(exe))
                    exe = Path.Combine(path, "xenia_canary.exe");
            }

            if (!File.Exists(exe))
                return null;
			
			string romdir = Path.GetDirectoryName(rom);
			
			if (Path.GetExtension(rom).ToLower() == ".m3u")
                {
                    rom = File.ReadAllText(rom);
                    rom = Path.Combine(romdir, rom.Substring(1));
                }

            SetupConfiguration(path);

            return new ProcessStartInfo()
            {
                FileName = exe,
                Arguments = "--fullscreen \"" + rom + "\"",
                WorkingDirectory = path,
            };
        }

        //Setup toml configuration file (using AppendValue because config file is very sensitive to values that do not exist and both emulators are still under heavy development)
        private void SetupConfiguration(string path)
        {
            try
            {
                using (IniFile ini = new IniFile(Path.Combine(path, _canary ? "xenia-canary.config.toml" : "xenia.config.toml"), IniOptions.KeepEmptyLines | IniOptions.UseSpaces))
                {
                    //APU section
                    string audio_driver = "\"" + SystemConfig["apu"] + "\"";
                    if (SystemConfig.isOptSet("apu") && !string.IsNullOrEmpty(SystemConfig["apu"]))
                        ini.AppendValue("APU", "apu", audio_driver);
                    else
                        ini.AppendValue("APU", "apu", "\"any\"");

                    //Content section
                    if (SystemConfig.isOptSet("license_mask") && !string.IsNullOrEmpty(SystemConfig["license_mask"]))
                        ini.AppendValue("Content", "license_mask", SystemConfig["license_mask"]);
                    else
                        ini.AppendValue("Content", "license_mask", "0");

                    //D3D12 section
                    if (SystemConfig.isOptSet("d3d12_clear_memory_page_state") && !string.IsNullOrEmpty(SystemConfig["d3d12_clear_memory_page_state"]))
                        ini.AppendValue("D3D12", "d3d12_clear_memory_page_state", SystemConfig["d3d12_clear_memory_page_state"]);
                    else
                        ini.AppendValue("D3D12", "d3d12_clear_memory_page_state", "false");

                    if (SystemConfig.isOptSet("d3d12_allow_variable_refresh_rate_and_tearing") && !string.IsNullOrEmpty(SystemConfig["d3d12_allow_variable_refresh_rate_and_tearing"]))
                        ini.AppendValue("D3D12", "d3d12_allow_variable_refresh_rate_and_tearing", SystemConfig["d3d12_allow_variable_refresh_rate_and_tearing"]);
                    else
                        ini.AppendValue("D3D12", "d3d12_allow_variable_refresh_rate_and_tearing", "true");

                    //Display section
                    string fxaa = "\"" + SystemConfig["postprocess_antialiasing"] + "\"";
                    if (SystemConfig.isOptSet("postprocess_antialiasing") && !string.IsNullOrEmpty(SystemConfig["postprocess_antialiasing"]))
                        ini.AppendValue("Display", "postprocess_antialiasing", fxaa);
                    else
                        ini.AppendValue("Display", "postprocess_antialiasing", "\"\"");

                    if (SystemConfig.isOptSet("internal_display_resolution") && !string.IsNullOrEmpty(SystemConfig["internal_display_resolution"]))
                        ini.AppendValue("Display", "internal_display_resolution", SystemConfig["internal_display_resolution"]);
                    else
                        ini.AppendValue("Display", "internal_display_resolution", "8");

                    //GPU section
                    string video_driver = "\"" + SystemConfig["gpu"] + "\"";
                    if (SystemConfig.isOptSet("gpu") && !string.IsNullOrEmpty(SystemConfig["gpu"]))
                        ini.AppendValue("GPU", "gpu", video_driver);
                    else
                        ini.AppendValue("GPU", "gpu", "\"any\"");

                    if (SystemConfig.isOptSet("render_target_path") && (SystemConfig["render_target_path"] == "rtv"))
                    {
                        ini.AppendValue("GPU", "render_target_path_d3d12", "\"rtv\"");
                        ini.AppendValue("GPU", "render_target_path_vulkan", "\"fbo\"");
                    }
                    else if (SystemConfig.isOptSet("render_target_path") && (SystemConfig["render_target_path"] == "rov"))
                    {
                        ini.AppendValue("GPU", "render_target_path_d3d12", "\"rov\"");
                        ini.AppendValue("GPU", "render_target_path_vulkan", "\"fsi\"");
                    }
                    else
                    {
                        ini.AppendValue("GPU", "render_target_path_d3d12", "\"\"");
                        ini.AppendValue("GPU", "render_target_path_vulkan", "\"\"");
                    }

                    if (SystemConfig.isOptSet("gpu_allow_invalid_fetch_constants") && !string.IsNullOrEmpty(SystemConfig["gpu_allow_invalid_fetch_constants"]))
                        ini.AppendValue("GPU", "gpu_allow_invalid_fetch_constants", SystemConfig["gpu_allow_invalid_fetch_constants"]);
                    else
                        ini.AppendValue("GPU", "gpu_allow_invalid_fetch_constants", "false");

                    if (SystemConfig.isOptSet("vsync") && !string.IsNullOrEmpty(SystemConfig["vsync"]))
                        ini.AppendValue("GPU", "vsync", SystemConfig["vsync"]);
                    else
                        ini.AppendValue("GPU", "vsync", "true");

                    //Memory section
                    if (SystemConfig.isOptSet("scribble_heap") && !string.IsNullOrEmpty(SystemConfig["scribble_heap"]))
                        ini.AppendValue("Memory", "scribble_heap", SystemConfig["scribble_heap"]);
                    else
                        ini.AppendValue("Memory", "scribble_heap", "false");

                    if (SystemConfig.isOptSet("protect_zero") && !string.IsNullOrEmpty(SystemConfig["protect_zero"]))
                        ini.AppendValue("Memory", "protect_zero", SystemConfig["protect_zero"]);
                    else
                        ini.AppendValue("Memory", "protect_zero", "true");

                    //Storage section
                    if (SystemConfig.isOptSet("mount_cache") && !string.IsNullOrEmpty(SystemConfig["mount_cache"]))
                        ini.AppendValue("Storage", "mount_cache", SystemConfig["mount_cache"]);
                    else
                        ini.AppendValue("Storage", "mount_cache", "false");

                    //Controllers section (HID)
                    if (SystemConfig.isOptSet("xenia_hid") && !string.IsNullOrEmpty(SystemConfig["xenia_hid"]))
                        ini.AppendValue("HID", "hid", SystemConfig["xenia_hid"]);
                    else
                        ini.AppendValue("HID", "hid", "any");
                }
            }
            catch { }
         }
    }
}

using Gum.Forms.Controls;
using Gum.GueDeriving;
using System;
var label = new Label { Text = "x" };
Console.WriteLine(label.Visual?.GetType().FullName);
if (label.Visual is TextRuntime tr)
  Console.WriteLine("is TextRuntime Outline=" + tr.OutlineThickness);
foreach (var p in label.Visual.GetType().GetProperties())
  if (p.Name.Contains("Text") || p.Name.Contains("Outline") || p.Name.Contains("Font") || p.Name.Contains("Color"))
    Console.WriteLine(p.Name);

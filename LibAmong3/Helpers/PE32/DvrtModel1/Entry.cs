using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LibAmong3.Helpers.PE32.DvrtModel1
{
    public class Entry
    {
        [XmlAttribute] public string? Offset { get; set; }
        [XmlAttribute] public string? Type { get; set; }
        [XmlAttribute] public string? Size { get; set; }
        [XmlAttribute] public string? Value { get; set; }
        [XmlAttribute] public string? Sign { get; set; }
        [XmlAttribute] public string? Scale { get; set; }
    }
}

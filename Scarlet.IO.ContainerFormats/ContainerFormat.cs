using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    public abstract class ContainerElement
    {
        protected ContainerElement() { }

        public virtual string GetName()
        {
            return "NoName";
        }

        public abstract Stream GetStream(Stream containerStream);
    }

    public abstract class ContainerFormat : FileFormat
    {
        protected ContainerFormat() : base() { }

        public abstract int GetElementCount();

        public IEnumerable<ContainerElement> GetElements(Stream containerStream)
        {
            List<ContainerElement> elements = new List<ContainerElement>();
            for (int i = 0; i < GetElementCount(); i++) elements.Add(GetElement(containerStream, i));
            return elements;
        }

        protected abstract ContainerElement GetElement(Stream containerStream, int elementIndex);
    }
}

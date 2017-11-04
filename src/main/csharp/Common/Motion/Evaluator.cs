using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SebastianHaeni.ThermoBox.Common.Motion
{
    internal class Evaluator
    {
        private readonly DetectorState[] _states;
        private readonly Size _size;

        public IEnumerable<Rectangle> BoundingBoxes { private get; set; }

        public Evaluator(DetectorState[] states, Size size)
        {
            _states = states;
            _size = size;
        }

        public DetectorState Evaluate()
        {
            if (!BoundingBoxes.Any())
            {
                return GetState(DetectorState.Nothing);
            }

            var first = BoundingBoxes.First();

            var threshold = _size.Width / 100;
            var leftBound = first.X < threshold;
            var rightBound = first.X + first.Width > _size.Width - threshold;

            if (leftBound == rightBound)
            {
                return GetState(DetectorState.Nothing);
            }

            var lastWidth = first.Width;

            var indicator = 0;

            foreach (var box in BoundingBoxes)
            {
                if (box.Width > lastWidth)
                {
                    indicator++;
                }
                else if (box.Width < lastWidth)
                {
                    indicator--;
                }

                lastWidth = box.Width;
            }

            var referenceCount = BoundingBoxes.Count() - 1;

            if (indicator == referenceCount)
            {
                return GetState(DetectorState.Entry);
            }

            if (indicator == -referenceCount)
            {
                return GetState(DetectorState.Exit);
            }

            return GetState(DetectorState.Nothing);
        }

        private DetectorState GetState(DetectorState state)
        {
            return _states.Contains(state) ? state : _states.First();
        }
    }
}

namespace KinectHandTracker
{
    class MeanFilter
    {
        private bool isInitialized;
        private int size;
        private float[] data;
        private float filteredResult;

        public float Value
        {
            get
            {
                return filteredResult;
            }
        }

        public MeanFilter(int size)
        {
            isInitialized = true;
            this.size = size;
            data = new float[size];
        }

        public float Update(float value)
        {
            if (isInitialized)
            {
                for (int i = 0; i < size; i++)
                {
                    data[i] = value;
                }
                isInitialized = false;
                return value;
            }
            else
            {
                float sum = 0;
                for (int i = 0; i < size - 1; i++)
                {
                    data[i] = data[i + 1];
                    sum += data[i];
                }
                data[size - 1] = value;
                sum += value;
                filteredResult = sum / size;
                return filteredResult;
            }
        }

        public void Clear()
        {
            isInitialized = true;
        }
    }
}

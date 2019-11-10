

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GeneticAlgorithm_TravelingSalesmanCsharp.Concrete {

    class Worker<T1,T2>
    {

        public bool IsRunning {get; private set;} = false;
        Func<T1,T2> Function {get;set;}

        object jobLock = new Object();
        List<List<T1>> Jobs {get;set;} = new List<List<T1>>();
        
        public bool HasResults {
            get {
                bool value;
                lock(resultLock) {
                    value = Results.Count>0;
                }
                return value;
            }
        }
        object resultLock = new Object();
        List<List<T2>> Results {get;set;} = new List<List<T2>>();

        int JobsCompleted {get;set;} = 0;

        object jobrunningLock = new Object();
        public bool JobRunning {
            get {
                bool value;
                lock(jobrunningLock) {
                    value = _JobRunning;
                }
                return value;
            }
            private set {
                lock(jobrunningLock) {
                    _JobRunning = value;
                }
            }
        }
        private bool _JobRunning = false; 

        Thread thread;

        public Worker(Func<T1,T2> function)
        {
            this.Function = function;
            thread = new Thread(new ThreadStart(run));
        }

        public void start() {
            if (!IsRunning) {
                IsRunning = true;
                thread.Start();
            }
        }

        public void stop() {
            IsRunning=false;
        }

        private void run() {
            bool hasJobs = false;
            while (IsRunning) {
                lock(jobLock) {
                    hasJobs = Jobs.Count>0;
                }
                if (hasJobs) {
                    JobRunning=true;
                    lock(jobLock) {
                        while (Jobs.Count>0) {
                            
                            List<T1> job = Jobs[0];
                            List<T2> result = new List<T2>();
                            Jobs.RemoveAt(0);
                            foreach(T1 j in job) {
                                result.Add(this.Function(j));
                            }
                            lock(resultLock) {
                                Results.Add(result);
                                JobsCompleted += 1;
                            }
                        }
                        JobRunning=false;
                    }
                } else {
                    Thread.Sleep(500);
                }
            }
        }

        
        public void addJobs(List<T1> jobs) {
            lock (jobLock) {
                this.Jobs.Add(jobs);
                JobRunning=true;
            }
        }

        public List<T2> GetResults() {
            if (Results.Count==0) return null;
            List<T2> value = null;
            lock(resultLock) {
                value = Results[0];
                Results.RemoveAt(0);
            }
            return value;
        }
        
    }

    class Vector {

        public int Dimension {
            get {
                return position.Count;
            }
        }

        public readonly List<int> position;

        public Vector(List<int> position) {
            if (position==null) throw new NullReferenceException("position cannot be null");
            this.position = position;
        }
        public Vector(int[] position) {
            if (position==null) throw new NullReferenceException("position cannot be null");
            this.position = position.ToList();
        }

        public decimal distance(Vector other) {
            return Vector.distance(this,other);
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //
            
            if (obj == null) {
                return false;
            } else if (GetType() != obj.GetType())
            {
                return false;
            }
            
            // TODO: write your implementation of Equals() here
            IEnumerable<int> other = obj as IEnumerable<int>;
            if (other!=null) {
                return this.position.SequenceEqual((IEnumerable<int>)obj);
            }

            return false;
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: write your implementation of GetHashCode() here
            return this.position.GetHashCode();
        }

        public static decimal distance(Vector vec1, Vector vec2) {
            decimal value = 0;
            int size = Math.Min(vec1.Dimension,vec2.Dimension);

            for (int i=0; i<size; i++) {
                value += 
                    Common.Common.Sqrt(
                    Math.Abs(
                        Common.Common.Pow(vec1.position[i],2)-
                        Common.Common.Pow(vec2.position[i],2)));
            }

            return value;
        }

        public override string ToString() {
            string value = "[{0}]";
            value = string.Format(value,string.Join(',',position));
            return value;
        }
    }
    class Node {

        object id;
        public readonly Vector position;

        public Node(object id, Vector position) {
            this.id = id;
            this.position = position;
        }
        public Node(string value) {
            string[] info = value.TrimStart('[').TrimEnd(']').Split(' ',1);
            this.id = info[0];
            this.position=new Vector(Array.ConvertAll(info[1].Split(','),int.Parse));
        }

        public override bool Equals(object obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //
            
            if (obj == null)
            {
                return false;
            } else if (GetType() != obj.GetType()) {
                return object.Equals(this.position,obj) || object.Equals(this.id,obj);
            }
            
            // TODO: write your implementation of Equals() here
            return this.position == ((Node)obj).position;
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: write your implementation of GetHashCode() here
            return position.GetHashCode();
        }

        public override string ToString() {
            string value = string.Format("{0} {1}",this.id,this.position);
            return value;
        }
    }

    class TravelingSpecimen:IComparable {

        protected int getNewId() {
            int new_id = next_id;
            next_id++;
            return new_id;
        }
        private static int next_id = 0;

        public List<Node> value;
        public object id;
        public object parentIdA;
        public object parentIdB;
        public int mutationCount;

        public decimal fit { get; internal set; }

        public TravelingSpecimen(Node[] value, object id = null, object parentIdA =null, object parentIdB = null, int mutationCount = 0) {
            this.value=value.ToList();
            this.id=(id!=null)?id:getNewId();
            this.parentIdA=parentIdA;
            this.parentIdB=parentIdB;
            this.mutationCount=mutationCount;
        }
        public TravelingSpecimen(List<Node> value, object id = null, object parentIdA =null, object parentIdB = null, int mutationCount = 0) {
            this.value=value;
            this.id=(id!=null)?id:getNewId();
            this.parentIdA=parentIdA;
            this.parentIdB=parentIdB;
            this.mutationCount=mutationCount;
        }

        public decimal distance() {
            decimal value = 0;

            for (int i=0; i<this.value.Count-1; i++) {
                value += this.value[i].position.distance(this.value[i+1].position);
            }

            return value;
        }

        public TravelingSpecimen[] combine(TravelingSpecimen other) {
            List<TravelingSpecimen> children = new List<TravelingSpecimen>();

            Random random = new Random();

            int min = 1;
            int max = this.value.Count-1;

            List<Node> newValue = this.value.ToArray().ToList();

            bool hasMutation = random.NextDouble()<0.06;
            int mutCount = Math.Max(this.mutationCount,other.mutationCount);

            if (hasMutation) {
                int mutInd1 = random.Next(min,max);
                int? mutInd2 = null;
                while (!mutInd2.HasValue || mutInd2.Value==mutInd1) {
                    mutInd2 = random.Next(min,max);
                }
                Node v1 = newValue[mutInd1];
                Node v2 = newValue[mutInd2.Value];

                newValue[mutInd1] = v2;
                newValue[mutInd2.Value] = v1;

                mutCount++;
            }

            List<int> indices = new List<int>();

            while (indices.Count<newValue.Count/2) {
                int? ind = null;
                while (!ind.HasValue || newValue[0] == other.value[ind.Value] || indices.Contains(ind.Value)) {
                    ind = random.Next(min,max);
                }
                indices.Add(ind.Value);
            }

            int i=0;
            for (i=0; i<indices.Count; i++) {
                int? ind = null;
                for (int j=1; j<newValue.Count-1; j++) {
                    if (newValue[j]==other.value[indices[i]]) {
                        ind = j;
                        break;
                    }
                }
                if (!ind.HasValue) {
                    throw new Exception(string.Format("Error {0} not in {1}",other.value[indices[i]],newValue.ToString()));
                } else {
                    newValue[ind.Value] = null;
                }
            }

            indices.Sort();
            indices.Reverse();

            i=0;
            while (i<indices.Count) {
                int ind = indices[i];
                int j = ind+1;
                while (j<newValue.Count) {
                    if (newValue[j]==null) {
                        newValue.RemoveAt(j);
                    } else {
                        j++;
                    }
                }
                newValue.Insert(ind,other.value[ind]);
                i++;
            }

            i=0;
            while (i<newValue.Count) {
                if (newValue[i]==null)
                    newValue.RemoveAt(i);
                else
                    i++;
            }

            foreach (Node val in this.value) {
                if (!newValue.Contains(val)) {
                    string output = string.Format("Error: Missing Value {0}\n{1}\n{2}",val,this.value.ToString(),newValue.ToString());
                    throw new Exception(output);
                }
            }

            children.Add(new TravelingSpecimen(newValue,null,this.id,other.id,mutCount));
            if (hasMutation) {
                children.AddRange(combine(other));
            }
            return children.ToArray();
        }

        public int CompareTo(object obj)
        {   
            if (obj==null) return -1;
            else if (obj.GetType() == this.GetType()) return this.fit.CompareTo(((TravelingSpecimen)obj).fit);
            else return fit.CompareTo(value);
        }

        public override string ToString() {
            string value = string.Format("Id: {0} Mutation Count: {1} Parent A: {2} Parent B: {3} Values: [{4}]",
                this.id,this.mutationCount,this.parentIdA,this.parentIdB,this.value);
            return value;
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //
            
            if (obj == null)
            {
                return false;
            } else if (GetType() != obj.GetType()) {
                return this.value.Equals(obj);
            }
            
            // TODO: write your implementation of Equals() here
            return this.value.Equals(((TravelingSpecimen)obj).value);
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: write your implementation of GetHashCode() here
            return this.value.GetHashCode();
        }
    }
}
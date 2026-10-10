using UnityEngine;
using UnityEngine.UI;

namespace VectorWhitebox
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class LaboratoryUiGraphic : MaskableGraphic
    {
        public enum Shape { Frame, Diagram, EdgeMarks }
        public Shape shape;
        public Color accent = LaboratoryUiTheme.Accent;
        public float lineWidth = 1.5f;
        public float cornerCut = 12;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (shape == Shape.Frame)
            {
                float c = Mathf.Min(cornerCut, Mathf.Min(r.width, r.height) * .25f);
                Vector2[] points = { new Vector2(r.xMin+c,r.yMin), new Vector2(r.xMax-c,r.yMin),
                    new Vector2(r.xMax,r.yMin+c), new Vector2(r.xMax,r.yMax-c),
                    new Vector2(r.xMax-c,r.yMax), new Vector2(r.xMin+c,r.yMax),
                    new Vector2(r.xMin,r.yMax-c), new Vector2(r.xMin,r.yMin+c) };
                for(int i=0;i<points.Length;i++) Line(mesh,points[i],points[(i+1)%points.Length],color,lineWidth);
                Line(mesh,new Vector2(r.xMin+c,r.yMax),new Vector2(r.xMin+c+44,r.yMax),accent,3);
            }
            else if (shape == Shape.EdgeMarks)
            {
                for(int i=0;i<7;i++)
                    Line(mesh,new Vector2(r.xMin+i*13,r.yMin),new Vector2(r.xMin+i*13+7,r.yMin+15),color,2);
                Line(mesh,new Vector2(r.xMax-60,r.yMin+7),new Vector2(r.xMax,r.yMin+7),accent,2);
            }
            else
            {
                Vector2 center=r.center; float radius=Mathf.Min(r.width,r.height)*.35f;
                Ring(mesh,center,radius,color,1.2f,6,30);
                Ring(mesh,center,radius*.78f,color,1.2f,6,30);
                Ring(mesh,center,radius*.27f,accent,2.5f,8,22.5f);
                for(int i=0;i<4;i++)
                {
                    float angle=i*Mathf.PI*.5f;
                    Vector2 d=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    Vector2 side=new Vector2(-d.y,d.x);
                    Line(mesh,center+d*radius*.36f,center+d*radius*.63f,accent,2);
                    Line(mesh,center+d*radius*.63f,center+d*radius*.55f+side*radius*.075f,accent,2);
                    Line(mesh,center+d*radius*.63f,center+d*radius*.55f-side*radius*.075f,accent,2);
                    Line(mesh,center+d*radius*1.13f,center+d*radius*1.34f,color,1);
                }
                for(int i=0;i<3;i++)
                {
                    float x=r.xMin+18+i*18;
                    Line(mesh,new Vector2(x,r.yMax-18),new Vector2(x+8,r.yMax-18),accent,3);
                }
                Line(mesh,new Vector2(r.xMin+18,r.yMin+25),new Vector2(r.xMax-18,r.yMin+25),color,1);
            }
        }

        static void Ring(VertexHelper mesh,Vector2 center,float radius,Color tint,float width,int sides,float offset)
        {
            for(int i=0;i<sides;i++)
            {
                float a=(offset+360f*i/sides)*Mathf.Deg2Rad,b=(offset+360f*(i+1)/sides)*Mathf.Deg2Rad;
                Line(mesh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                    center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,tint,width);
            }
        }

        static void Line(VertexHelper mesh,Vector2 a,Vector2 b,Color tint,float width)
        {
            Vector2 direction=(b-a).normalized; Vector2 normal=new Vector2(-direction.y,direction.x)*width*.5f;
            int start=mesh.currentVertCount;
            mesh.AddVert(a-normal,tint,Vector2.zero);mesh.AddVert(a+normal,tint,Vector2.zero);
            mesh.AddVert(b+normal,tint,Vector2.zero);mesh.AddVert(b-normal,tint,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
    }
}

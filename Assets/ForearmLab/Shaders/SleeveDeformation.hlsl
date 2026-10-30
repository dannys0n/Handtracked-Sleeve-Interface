#ifndef FOREARM_SLEEVE_DEFORMATION
#define FOREARM_SLEEVE_DEFORMATION
// Shared by the shell, projected UI, glyphs, 3D payload, and projection tethers.
float4x4 _ProjectorWorldToLocal;
float4x4 _ProjectorLocalToWorld;
float _ProjectorLength;
float _CloseAmount;
float _FoldedCuffLength;
float _SleeveDetached;

float2 SleeveRing(float column)
{
    float angle=column*1.57079632679; // Four sectors, half-sector stagger on alternating rows.
    return float2(cos(angle),sin(angle));
}

float3 SleeveDeformLocal(float3 position)
{
    float radius=length(position.xy);
    // Keep the cylinder axis finite, including vertices from arbitrary 3D content.
    float u=radius>.000001?atan2(position.y,position.x)/6.28318530718:0;
    float t=position.z/max(.001,_ProjectorLength);
    float row=min(7,floor(saturate(t)*8));
    float f=saturate(t)*8-row;
    float offset=fmod(row,2)*.5;
    float x=u*4-offset;
    float k=floor(x),r=frac(x);
    float column=k+offset;
    float2 folded;
    if(f<=2*min(r,1-r))
        folded=SleeveRing(column)*(1-r-.5*f)+SleeveRing(column+1)*(r-.5*f)+SleeveRing(column+.5)*f;
    else if(r<.5)
        folded=SleeveRing(column)*(1-f)+SleeveRing(column-.5)*(.5*f-r)+SleeveRing(column+.5)*(.5*f+r);
    else
        folded=SleeveRing(column+1)*(1-f)+SleeveRing(column+.5)*(1-r+.5*f)+SleeveRing(column+1.5)*(r-1+.5*f);
    float faceting=smoothstep(0,.28,saturate(_CloseAmount));
    position.xy=lerp(position.xy,folded*radius,faceting);
    float closing=smoothstep(0,1,saturate(_CloseAmount));
    // Fold into a visible cuff without collapsing the radius or fading content.
    float cuffLength=clamp(_FoldedCuffLength,.005,_ProjectorLength);
    position.z=lerp(position.z,_ProjectorLength-(1-t)*cuffLength,closing);
    return position;
}

float3 SleeveDeformWorld(float3 worldPosition)
{
    if(_SleeveDetached>.5)return worldPosition;
    float3 local=mul(_ProjectorWorldToLocal,float4(worldPosition,1)).xyz;
    return mul(_ProjectorLocalToWorld,float4(SleeveDeformLocal(local),1)).xyz;
}
#endif

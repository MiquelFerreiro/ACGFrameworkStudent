#version 450 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;

uniform vec4 u_ambient_light;
uniform vec4 u_background_light;

uniform vec4 u_color;

uniform float u_abs_coef;

uniform float u_step_length;
uniform float u_noise_scale;
uniform float u_noise_detail;

out vec4 FragColor;

// Noise functions
float hash1( float n )
{
    return fract( n*17.0*fract( n*0.3183099 ) );
}

float noise( vec3 x )
{
    vec3 p = floor(x);
    vec3 w = fract(x);
    
    vec3 u = w*w*w*(w*(w*6.0-15.0)+10.0);
    
    float n = p.x + 317.0*p.y + 157.0*p.z;
    
    float a = hash1(n+0.0);
    float b = hash1(n+1.0);
    float c = hash1(n+317.0);
    float d = hash1(n+318.0);
    float e = hash1(n+157.0);
    float f = hash1(n+158.0);
    float g = hash1(n+474.0);
    float h = hash1(n+475.0);

    float k0 =   a;
    float k1 =   b - a;
    float k2 =   c - a;
    float k3 =   e - a;
    float k4 =   a - b - c + d;
    float k5 =   a - c - e + g;
    float k6 =   a - b - e + f;
    float k7 = - a + b + c - d + e - f - g + h;

    return -1.0+2.0*(k0 + k1*u.x + k2*u.y + k3*u.z + k4*u.x*u.y + k5*u.y*u.z + k6*u.z*u.x + k7*u.x*u.y*u.z);
}

#define MAX_OCTAVES 16

float fractal_noise( vec3 P, float detail )
{
    float fscale = 1.0;
    float amp = 1.0;
    float sum = 0.0;
    float octaves = clamp(detail, 0.0, 16.0);
    int n = int(octaves);

    for (int i = 0; i <= MAX_OCTAVES; i++) {
        if (i > n) continue;
        float t = noise(fscale * P);
        sum += t * amp;
        amp *= 0.5;
        fscale *= 2.0;
    }

    return sum;
}

float cnoise( vec3 P, float scale, float detail )
{
    P *= scale;
    return clamp(fractal_noise(P, detail), 0.0, 1.0);
}

void main() {

    vec3 rayOrigin = u_camera_position;
    vec3 rayDir = normalize(v_world_position - u_camera_position);

    vec3 boxMin = vec3(-1);
    vec3 boxMax = vec3(1);

    // Intersect AABB
    vec3 tMin = (boxMin - rayOrigin) / rayDir;
    vec3 tMax = (boxMax - rayOrigin) / rayDir;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);

    // Initialize variables
    float thickness = 0.0;
    vec3 position = rayOrigin + tNear * rayDir;
    vec3 accumulated_radiance = vec3(0.0);
    float accumulated_transmittance = 1.0;

    // Ray-marching loop for emission-absorption
    for (float t = tNear; t < tFar; t += u_step_length) {
        //float density = noise(position * u_noise_scale);
        float density = noise(position) + cnoise(position, u_noise_scale, u_noise_detail) + fractal_noise(position, u_noise_detail);
        float absorption = density * u_abs_coef;

        // Transmittance at this step
        float step_transmittance = exp(-absorption * u_step_length);
        
        // Accumulate emitted radiance
        accumulated_radiance += accumulated_transmittance * step_transmittance * u_color.rgb * density;

        // Update accumulated transmittance
        accumulated_transmittance *= step_transmittance;

        // Advance position along the ray
        position += rayDir * u_step_length;
    }

    // Combine accumulated radiance with background color
    FragColor = vec4(accumulated_radiance + u_background_light.rgb * accumulated_transmittance, 1.0);
}

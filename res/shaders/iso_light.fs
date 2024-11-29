#version 450 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;

uniform vec4 u_ambient_light;
uniform vec4 u_background_light;
uniform vec4 u_color;

uniform mat4 u_model;

uniform float u_step_length;

uniform sampler3D u_texture;  // 3D texture for density data

uniform float u_h;

uniform float u_light_intensity;
uniform vec4 u_light_color;
uniform vec3 u_light_direction;
uniform vec3 u_light_position;
uniform vec3 u_local_light_position;


out vec4 FragColor;

// ---------------------------------------------------------------------------------------------------//

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

    vec3 position = rayOrigin + tNear * rayDir;
    vec4 final_color = vec4(0.0);
    float density;

    bool fin = false;

    // Ray-marching loop for emission-absorption
    for (float t = tNear; t < tFar; t += u_step_length) {

        vec3 localPosition = (inverse(u_model) * vec4(position, 1.0)).xyz;
        vec3 texCoords = (localPosition + vec3(1.0)) * 0.5; // Map to [0, 1] range
        density = texture(u_texture, texCoords).x;
        
        if (density != 0) {
            
            vec3 gradient = (1 / (2 * u_h)) * vec3(

                texture(u_texture, texCoords + vec3(u_h,0,0) ).x - texture(u_texture, texCoords + vec3(-u_h,0,0) ).x ,
                texture(u_texture, texCoords + vec3(0,u_h,0) ).x - texture(u_texture, texCoords + vec3(0,-u_h,0) ).x ,
                texture(u_texture, texCoords + vec3(0,0,u_h) ).x - texture(u_texture, texCoords + vec3(0,0,-u_h) ).x );

            vec3 normal = - normalize(gradient);

            final_color = vec4(normal.x, normal.y, normal.z, 1.0);

            vec3 wo = - normalize(rayDir);

            vec3 wi = normalize(u_light_position - position);

            float visibility = 1.0;

            vec4 radiance;

            float dist = distance(u_light_position, position);

            float tot_steps = dist / u_step_length;

            for (float t = 1; t < tot_steps; t += u_step_length) {

                vec3 pos2 = position + t * wi * u_step_length;

                vec3 localPosition2 = (inverse(u_model) * vec4(pos2, 1.0)).xyz;
                vec3 texCoords2 = (localPosition2 + vec3(1.0)) * 0.5; // Map to [0, 1] range
                float density2 = texture(u_texture, texCoords2).x;

                if ( density2 != 0 ) {
                    visibility = 0.0;
                    break;
                }
            }

            vec3 ambient = vec3(0.1);   // Luz ambiental
            vec3 kd = vec3(0.8);        // Coeficiente difuso
            vec3 ks = vec3(0.5);        // Coeficiente especular
            float alpha = 1.0;    // Brillo especular (shininess)

            vec3 light_color = vec3(1.0);

            vec3 wr = 2 * dot(wi, normal) * normal - wi;

            vec3 phong_color = kd / 3.1416 + (3.1416 * 2 / (alpha +1)) * ks * pow(dot(wr, wo), alpha);

            radiance = visibility  * dot(wi, normal) * light_color * phong_color + ambient;

            FragColor = radiance;

            fin = true;

        }

        // Advance position along the ray
        position += rayDir * u_step_length;

    }

    if (!fin) {
            FragColor = u_background_light;
    }


}
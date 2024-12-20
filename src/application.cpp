#include "application.h"

#include <algorithm>


bool render_wireframe = false;
Camera* Application::camera = nullptr;

void Application::init(GLFWwindow* window)
{
    this->instance = this;
    glfwGetFramebufferSize(window, &this->window_width, &this->window_height);

    // OpenGL flags
    glEnable(GL_CULL_FACE); // render both sides of every triangle
    glEnable(GL_DEPTH_TEST); // check the occlusions using the Z buffer

    // Create camera
    this->camera = new Camera();
    this->camera->lookAt(glm::vec3(1.f, 1.5f, 4.f), glm::vec3(0.f, 0.0f, 0.f), glm::vec3(0.f, 1.f, 0.f));
    this->camera->setPerspective(60.f, this->window_width / (float)this->window_height, 0.1f, 500.f); // set the projection, we want to be perspective

    this->flag_grid = false;
    this->flag_wireframe = false;

    this->ambient_light = glm::vec4(1, 1, 1, 1.0f);

    this->background_light = glm::vec4(219 / 255.0f, 237 / 255.0f, 242 / 255.0f, 1.0f);
    //this->background_light = glm::vec4(0.0f, 0.0f, 0.0f, 1.0f);


    ////////////////////////// LAB 3 ////////////////////////////////

    /*SceneNode* volume = new SceneNode("Volume Node");
    volume->mesh = Mesh::Get("res/meshes/cube.obj");

    VolumeMaterial* mat = new VolumeMaterial();
    
    mat->color = glm::vec4(151 / 255.0f, 57 / 255.0f, 196 / 255.0f, 1.0f);

    volume->material = mat;

    this->node_list.push_back(volume);*/

    ////////////////////////// LAB 4 ////////////////////////////////
    //
    //SceneNode* volume = new SceneNode("Volume Node");
    //volume->mesh = Mesh::Get("res/meshes/cube.obj");

    //RabbitMaterial* mat = new RabbitMaterial();

    //mat->color = glm::vec4(0.0f);

    //// absolute path miquel
    ////char path[] = "C:/ACG/ACGFrameworkStudent/res/bunny_cloud.vdb";

    //// absolute path alex
    //char path[] = "C:/Users/alexf/Documents/GitHub/ACGFrameworkStudent/res/bunny_cloud.vdb";

    ////char path[] = "/res/bunny_cloud.vdb";

    //mat->loadVDB(path);

    //volume->material = mat;

    //this->node_list.push_back(volume);
    //   
    //// LIGHT

    //glm::vec3 light_pos = glm::vec3(1.5f);

    //Light* light = new Light(light_pos, LIGHT_POINT);

    //this->light_list.push_back(light);

    //this->node_list.push_back(light);



    // ///////////////////// LAB 5 ///////////////////////// //

    SceneNode* volume = new SceneNode("Isosurface");
    volume->mesh = Mesh::Get("res/meshes/cube.obj");

    IsosurfaceMaterial* mat = new IsosurfaceMaterial();

    mat->color = glm::vec4(0.0f, 1.0f, 0.0f, 1.0f);

    // absolute path miquel
    char path[] = "C:/ACG/ACGFrameworkStudent/res/bunny_cloud.vdb";

    // absolute path alex
    //char path[] = "C:/Users/alexf/Documents/GitHub/ACGFrameworkStudent/res/bunny_cloud.vdb";

    //char path[] = "/res/bunny_cloud.vdb";

    mat->loadVDB(path);

    volume->material = mat;

    this->node_list.push_back(volume);

    // LIGHT

    glm::vec3 light_pos = glm::vec3(1.5f);

    Light* light = new Light(light_pos, LIGHT_POINT);

    light->intensity = 3.0f;

    this->light_list.push_back(light);

    this->node_list.push_back(light);


}

void Application::update(float dt)
{
    // mouse update
    glm::vec2 delta = this->lastMousePosition - this->mousePosition;
    if (this->dragging) {
        this->camera->orbit(-delta.x * dt, delta.y * dt);
    }
    this->lastMousePosition = this->mousePosition;

    // Move light source
    {
        static float angle = 0.0f;
        angle += dt / 3;

        float radius = 4.0f;

        float x = radius * cos(angle);
        float z = radius * sin(angle);

        this->light_list[0]->model = glm::translate(glm::mat4(1.0f), glm::vec3(x, 1.0f, z));
    }
}

void Application::render()
{
    // set the clear color (the background color)
    glClearColor(background_light.x, background_light.y, background_light.z, 1.0f);

    // Clear the window and the depth buffer
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    // set flags
    glEnable(GL_DEPTH_TEST);
    glEnable(GL_CULL_FACE);

    for (unsigned int i = 0; i < this->node_list.size(); i++)
    {
        
        this->node_list[i]->render(this->camera);

        if (this->flag_wireframe) this->node_list[i]->renderWireframe(this->camera);
    }

    // Draw the floor grid
    if (this->flag_grid) drawGrid();
}

void Application::renderGUI()
{
    if (ImGui::TreeNodeEx("Scene", ImGuiTreeNodeFlags_DefaultOpen))
    {

        ImGui::ColorEdit3("Ambient light", (float*)&this->ambient_light);
        ImGui::ColorEdit3("Background light", (float*)&this->background_light);


        if (ImGui::TreeNode("Camera")) {
            this->camera->renderInMenu();
            ImGui::TreePop();
        }

        unsigned int count = 0;
        std::stringstream ss;
        for (auto& node : this->node_list) {
            ss << count;
            if (ImGui::TreeNode(node->name.c_str())) {
                node->renderInMenu();
                ImGui::TreePop();
            }
        }
        ImGui::TreePop();
    }
}

void Application::shutdown() { }

// keycodes: https://www.glfw.org/docs/3.3/group__keys.html
void Application::onKeyDown(int key, int scancode)
{
    switch (key) {
    case GLFW_KEY_ESCAPE: // quit
        close = true;
        break;
    case GLFW_KEY_R:
        Shader::ReloadAll();
        break;
    }
}

// keycodes: https://www.glfw.org/docs/3.3/group__keys.html
void Application::onKeyUp(int key, int scancode)
{
    switch (key) {
    case GLFW_KEY_T:
        std::cout << "T released" << std::endl;
        break;
    }
}

void Application::onRightMouseDown()
{
    this->dragging = true;
    this->lastMousePosition = this->mousePosition;
}

void Application::onRightMouseUp()
{
    this->dragging = false;
    this->lastMousePosition = this->mousePosition;
}

void Application::onLeftMouseDown()
{
    this->dragging = true;
    this->lastMousePosition = this->mousePosition;
}

void Application::onLeftMouseUp()
{
    this->dragging = false;
    this->lastMousePosition = this->mousePosition;
}

void Application::onMiddleMouseDown() { }

void Application::onMiddleMouseUp() { }

void Application::onMousePosition(double xpos, double ypos) { }

void Application::onScroll(double xOffset, double yOffset)
{
    int min = this->camera->min_fov;
    int max = this->camera->max_fov;

    if (yOffset < 0) {
        this->camera->fov += 4.f;
        if (this->camera->fov > max) {
            this->camera->fov = max;
        }
    }
    else {
        this->camera->fov -= 4.f;
        if (this->camera->fov < min) {
            this->camera->fov = min;
        }
    }
    this->camera->updateProjectionMatrix();
}
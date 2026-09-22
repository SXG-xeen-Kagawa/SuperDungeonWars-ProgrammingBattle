using UnityEngine;

public static class RuntimeBoxMeshFactory
{
    public enum SurfaceType
    {
        CorridorFloor = 0,
        RoomFloor = 1,
        CentralHallFloor = 2,
        Wall = 3,
        Ceiling = 4,
        Fallback = 5,
    }

    public static Mesh CreateBoxMesh(
        string meshName,
        Vector3 size,
        SurfaceType topSurfaceType,
        SurfaceType sideSurfaceType,
        SurfaceType bottomSurfaceType)
    {
        float halfX = size.x * 0.5f;
        float halfY = size.y * 0.5f;
        float halfZ = size.z * 0.5f;

        Vector3[] vertices = new Vector3[24];
        Vector3[] normals = new Vector3[24];
        Vector2[] uvs = new Vector2[24];
        Vector2[] surfaceTypes = new Vector2[24];
        int[] triangles = new int[36];

        int vertexIndex = 0;
        int triangleIndex = 0;

        AddFace(
            vertices,
            normals,
            uvs,
            surfaceTypes,
            triangles,
            ref vertexIndex,
            ref triangleIndex,
            new Vector3(-halfX, halfY, -halfZ),
            new Vector3(-halfX, halfY, halfZ),
            new Vector3(halfX, halfY, halfZ),
            new Vector3(halfX, halfY, -halfZ),
            Vector3.up,
            topSurfaceType);

        AddFace(
            vertices,
            normals,
            uvs,
            surfaceTypes,
            triangles,
            ref vertexIndex,
            ref triangleIndex,
            new Vector3(-halfX, -halfY, halfZ),
            new Vector3(-halfX, -halfY, -halfZ),
            new Vector3(halfX, -halfY, -halfZ),
            new Vector3(halfX, -halfY, halfZ),
            Vector3.down,
            bottomSurfaceType);

        AddFace(
            vertices,
            normals,
            uvs,
            surfaceTypes,
            triangles,
            ref vertexIndex,
            ref triangleIndex,
            new Vector3(-halfX, -halfY, halfZ),
            new Vector3(halfX, -halfY, halfZ),
            new Vector3(halfX, halfY, halfZ),
            new Vector3(-halfX, halfY, halfZ),
            Vector3.forward,
            sideSurfaceType);

        AddFace(
            vertices,
            normals,
            uvs,
            surfaceTypes,
            triangles,
            ref vertexIndex,
            ref triangleIndex,
            new Vector3(halfX, -halfY, -halfZ),
            new Vector3(-halfX, -halfY, -halfZ),
            new Vector3(-halfX, halfY, -halfZ),
            new Vector3(halfX, halfY, -halfZ),
            Vector3.back,
            sideSurfaceType);

        AddFace(
            vertices,
            normals,
            uvs,
            surfaceTypes,
            triangles,
            ref vertexIndex,
            ref triangleIndex,
            new Vector3(halfX, -halfY, halfZ),
            new Vector3(halfX, -halfY, -halfZ),
            new Vector3(halfX, halfY, -halfZ),
            new Vector3(halfX, halfY, halfZ),
            Vector3.right,
            sideSurfaceType);

        AddFace(
            vertices,
            normals,
            uvs,
            surfaceTypes,
            triangles,
            ref vertexIndex,
            ref triangleIndex,
            new Vector3(-halfX, -halfY, -halfZ),
            new Vector3(-halfX, -halfY, halfZ),
            new Vector3(-halfX, halfY, halfZ),
            new Vector3(-halfX, halfY, -halfZ),
            Vector3.left,
            sideSurfaceType);

        Mesh mesh = new Mesh();
        mesh.name = meshName;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.uv2 = surfaceTypes;
        mesh.triangles = triangles;

        // UV と法線、および三角形の向きから、
        // Normal Map 用の Tangent / handedness を生成する。
        mesh.RecalculateTangents();

        mesh.RecalculateBounds();

        return mesh;
    }

    private static void AddFace(
        Vector3[] vertices,
        Vector3[] normals,
        Vector2[] uvs,
        Vector2[] surfaceTypes,
        int[] triangles,
        ref int vertexIndex,
        ref int triangleIndex,
        Vector3 vertex0,
        Vector3 vertex1,
        Vector3 vertex2,
        Vector3 vertex3,
        Vector3 normal,
        SurfaceType surfaceType)
    {
        vertices[vertexIndex + 0] = vertex0;
        vertices[vertexIndex + 1] = vertex1;
        vertices[vertexIndex + 2] = vertex2;
        vertices[vertexIndex + 3] = vertex3;

        normals[vertexIndex + 0] = normal;
        normals[vertexIndex + 1] = normal;
        normals[vertexIndex + 2] = normal;
        normals[vertexIndex + 3] = normal;

        uvs[vertexIndex + 0] = new Vector2(0.0f, 0.0f);
        uvs[vertexIndex + 1] = new Vector2(1.0f, 0.0f);
        uvs[vertexIndex + 2] = new Vector2(1.0f, 1.0f);
        uvs[vertexIndex + 3] = new Vector2(0.0f, 1.0f);

        Vector2 surfaceTypeUv = new Vector2(
            (float)surfaceType,
            0.0f);

        surfaceTypes[vertexIndex + 0] = surfaceTypeUv;
        surfaceTypes[vertexIndex + 1] = surfaceTypeUv;
        surfaceTypes[vertexIndex + 2] = surfaceTypeUv;
        surfaceTypes[vertexIndex + 3] = surfaceTypeUv;

        triangles[triangleIndex + 0] = vertexIndex + 0;
        triangles[triangleIndex + 1] = vertexIndex + 1;
        triangles[triangleIndex + 2] = vertexIndex + 2;
        triangles[triangleIndex + 3] = vertexIndex + 0;
        triangles[triangleIndex + 4] = vertexIndex + 2;
        triangles[triangleIndex + 5] = vertexIndex + 3;

        vertexIndex += 4;
        triangleIndex += 6;
    }
}